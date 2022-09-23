using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuickSpike;

class Lexer
{
    private static readonly TokenRule[] TokenRules = new[]
    {
        new TokenRule(new Regex(@"^(?:\r\n)+"), TokenType.NewLine),
        new TokenRule(new Regex(@"^\s+"), TokenType.WhiteSpace),
        new TokenRule(new Regex(@"^\("), TokenType.OpenParen),
        new TokenRule(new Regex(@"^\)"), TokenType.ClosedParen),
        new TokenRule(new Regex(@"^\{"), TokenType.OpenBrace),
        new TokenRule(new Regex(@"^\}"), TokenType.ClosedBrace),
        new TokenRule(new Regex(@"^="), TokenType.Assgn),
        new TokenRule(new Regex(@"^\+"), TokenType.Plus),
        new TokenRule(new Regex(@"^\,"), TokenType.Comma),
        new TokenRule(new Regex(@"^remote"), TokenType.Remote),
        new TokenRule(new Regex(@"^def"), TokenType.Def),
        new TokenRule(new Regex(@"^[1-9]\d*"), TokenType.IntConst),
        new TokenRule(new Regex(@"^[a-zA-Z\.]+"), TokenType.Identifier),
    };

    private readonly string source;
    private int currentIndex = 0;

    public Lexer(string source)
    {
        this.source = source;
    }

    public TokenStream Tokenize()
    {
        return new TokenStream(GetTokens().GetEnumerator());
    }

    IEnumerable<Token> GetTokens()
    {
        bool isDone = false;

        while (!isDone)
        {
            var result = TryGetNextToken(out Token token);
            if (!result)
            {
                throw new Exception($"Invalid syntax at '{source.AsSpan().Slice(currentIndex, Math.Min(10, source.Length - currentIndex))}'");
            }

            if (token.Type == TokenType.WhiteSpace)
            {
                continue;
            }

            isDone = token.Type == TokenType.Eof;
            yield return token;
        }
    }

    private bool TryGetNextToken(out Token token)
    {
        token = default;

        if (currentIndex == source.Length)
        {
            token = new Token("", TokenType.Eof);
            return true;
        }

        foreach (var rule in TokenRules)
        {
            var match = rule.Rule.Match(source, currentIndex, source.Length - currentIndex);
            if (!match.Success)
            {
                continue;
            }

            token = new Token(match.Value, rule.Type);

            currentIndex = match.Index + match.Value.Length;

            return true;
        }

        return false;
    }
}

record struct Token(string Value, TokenType Type);
record struct TokenRule(Regex Rule, TokenType Type);

enum TokenType
{
    Eof,
    Identifier,
    Remote,
    Def,
    IntConst,
    Assgn,
    Plus,
    OpenParen,
    ClosedParen,
    OpenBrace,
    ClosedBrace,
    Comma,
    NewLine,
    WhiteSpace
}

class Parser
{
    private TokenStream tokens;

    public Parser(TokenStream tokens)
    {
        this.tokens = tokens;
    }

    public Expression ParseExpression()
    {
        if (this.tokens.IsNextOfType(TokenType.Remote))
        {
            return this.ParseRemote();
        }

        if (this.tokens.IsNextOfType(TokenType.OpenBrace))
        {
            return this.ParseBlock();
        }

        Token token;
        if (this.tokens.TryPeekAfterNext(out token))
        {
            if (token.Type == TokenType.Assgn)
            {
                return this.ParseAssignment();
            }
            else if (token.Type == TokenType.Plus)
            {
                return this.ParseAdd();
            }
        }

        token = this.tokens.Consume();
        if (token.Type == TokenType.Identifier)
        {
            return new VarExpression(token.Value);
        }

        if (token.Type == TokenType.IntConst)
        {
            return new IntExpression(int.Parse(token.Value));
        }


        throw new Exception(string.Format("Unexpected token {0}", token));
    }

    private Expression ParseAssignment()
    {
        Token idToken = this.tokens.ConsumeType(TokenType.Identifier);
        this.tokens.ConsumeType(TokenType.Assgn);
        Expression expression = this.ParseExpression();

        return new AssignmentExpression(idToken.Value, expression);
    }

    private Expression ParseAdd()
    {
        Expression left = this.ParseOperandExpression();
        this.tokens.ConsumeType(TokenType.Plus);
        Expression right = this.ParseExpression();

        return new AddExpression(left, right);
    }

    private Expression ParseOperandExpression()
    {
        Token token = this.tokens.Consume();
        if (token.Type == TokenType.Identifier)
        {
            return new VarExpression(token.Value);
        }

        if (token.Type == TokenType.IntConst)
        {
            return new IntExpression(int.Parse(token.Value));
        }

        throw new Exception($"Unexpected operand token {token}");
    }

    private Expression ParseBlock()
    {
        this.tokens.ConsumeType(TokenType.OpenBrace);
        this.tokens.ConsumeType(TokenType.NewLine);
        var expressions = this.ParseExpressionList();
        this.tokens.ConsumeType(TokenType.ClosedBrace);
        return new BlockExpression(expressions);
    }

    private IReadOnlyCollection<Expression> ParseExpressionList()
    {
        List<Expression> expressions = new();
        expressions.Add(ParseExpression());
        while (this.tokens.TryConsumeType(TokenType.NewLine, out _))
        {
            if (this.tokens.IsNextOfType(TokenType.ClosedBrace))
            {
                return expressions;
            }

            expressions.Add(ParseExpression());
        }

        return expressions;
    }

    private Expression ParseRemote()
    {
        this.tokens.ConsumeType(TokenType.Remote);
        this.tokens.ConsumeType(TokenType.OpenParen);
        Expression expression = this.ParseExpression();
        this.tokens.ConsumeType(TokenType.ClosedParen);
        return new FutureValueExpression(expression);
    }
}

class TokenStream : IDisposable
{
    private bool eof = false;
    private IEnumerator<Token> iterator;
    private int pos = 0;
    private Queue<Token> peekBuffer = new Queue<Token>(2);

    public TokenStream(IEnumerator<Token> tokens)
    {
        this.iterator = tokens;
        this.pos = 0;
    }

    public int Pos => pos;

    public bool Eof => eof;

    public Token Consume()
    {
        if (eof)
        {
            throw new Exception("Cannot seek past end of stream.");
        }

        return ConsumeInternal();
    }

    public Token ConsumeType(TokenType type)
    {
        Token token = Consume();
        if (token.Type != type)
        {
            throw new Exception($"Expected {type} but go {token}");
        }

        return token;
    }

    public bool TryConsume(out Token token)
    {
        token = default;

        if (Eof)
        {
            return false;
        }

        token = ConsumeInternal();
        return true;
    }

    public bool TryConsumeType(TokenType type, out Token token)
    {
        token = default;
        if (!IsNextOfType(type))
        {
            return false;
        }

        return TryConsume(out token);
    }

    public bool TryPeekAfterNext(out Token token)
    {
        Debug.Assert(peekBuffer.Count <= 2);
        token = default;

        if (eof)
        {
            return false;
        }

        if (peekBuffer.Count == 2)
        {
            // TODO: use a queue that allows efficiently peeking ahead
            token = peekBuffer.ElementAt(1);
            return true;
        }

        if (peekBuffer.Count == 1)
        {
            // already have one token in the peek buffer, we only need to peek one more ahead
            if (peekBuffer.Peek().Type == TokenType.Eof)
            {
                return false;
            }

            token = PeekInternal();
            return true;
        }

        // no token buffered, so we have to peek twice
        if (PeekInternal().Type == TokenType.Eof)
        {
            return false;
        }

        token = PeekInternal();
        return true;
    }

    public bool TryPeek(out Token token)
    {
        if (TryGetPeeked(out token))
        {
            return true;
        }

        if (eof)
        {
            token = default;
            return false;
        }

        token = PeekInternal();
        return true;
    }
    
    public bool IsNextOfType(TokenType type)
    {
        return TryPeek(out Token token) && token.Type == type;
    }

    public void Dispose()
    {
        iterator.Dispose();
    }

    private bool TryGetPeeked(out Token peeked)
    {
        peeked = default;
        if (peekBuffer.Count == 0)
        {
            return false;
        }

        peeked = peekBuffer.Peek();
        return true;
    }

    private Token ConsumeInternal()
    {
        pos++;
        Token token;

        if (TryConsumePeeked(out token))
        {
            return token;
        }

        iterator.MoveNext();
        token = iterator.Current;

        eof = token.Type == TokenType.Eof;
        return token;
    }

    private Token PeekInternal()
    {
        iterator.MoveNext();
        Token token = iterator.Current;
        peekBuffer.Enqueue(token);
        return token;
    }

    private bool TryConsumePeeked(out Token peeked)
    {
        peeked = default;
        if (peekBuffer.Count == 0)
        {
            return false;
        }

        peeked = peekBuffer.Dequeue();
        return true;
    }
}
