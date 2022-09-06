using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickSpike;

class Lexer
{
    public TokenStream Tokenize(string input)
    {
        return new TokenStream(GetTokens(input));
    }

    List<Token> GetTokens(string input)
    {
        var tokens = input.Split(" ");
        return tokens.Select(t => t.Trim())
            .Where(t => t != "")
            .Select(value => GetToken(value))
            .ToList();
    }

    Token GetToken(string value)
    {
        if (value == "=")
        {
            return new Token(value, TokenType.Assgn);
        }

        if (value == "+")
        {
            return new Token(value, TokenType.Plus);
        }

        if (value == "remote")
        {
            return new Token(value, TokenType.Remote);
        }

        if (value == "(")
        {
            return new Token(value, TokenType.OpenParen);
        }

        if (value == ")")
        {
            return new Token(value, TokenType.ClosedParen);
        }

        if (int.TryParse(value, out _))
        {
            return new Token(value, TokenType.IntConst);
        }

        return new Token(value, TokenType.Id);
    }
}

record struct Token(string Value, TokenType Type);

enum TokenType
{
    Id,
    Remote,
    IntConst,
    Assgn,
    Plus,
    OpenParen,
    ClosedParen
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
        if (token.Type == TokenType.Id)
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
        Token idToken = this.tokens.ConsumeType(TokenType.Id);
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
        if (token.Type == TokenType.Id)
        {
            return new VarExpression(token.Value);
        }

        if (token.Type == TokenType.IntConst)
        {
            return new IntExpression(int.Parse(token.Value));
        }

        throw new Exception($"Unexpected operand token {token}");
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

class TokenStream
{
    public TokenStream(List<Token> tokens)
    {
        Tokens = tokens;
        Pos = 0;
    }

    public IReadOnlyList<Token> Tokens { get; private set; }
    public int Pos { get; private set; }
    public bool Eof => Pos == Tokens.Count;

    public Token Consume()
    {
        if (Eof)
        {
            throw new Exception("Cannot seek past end of stream.");
        }

        return this.Tokens[Pos++];
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

        token = this.Tokens[Pos++];
        return true;
    }

    public bool TryPeekAfterNext(out Token token)
    {
        token = default;

        if (Eof)
        {
            return false;
        }

        if (Pos + 1 >= Tokens.Count)
        {
            return false;
        }

        token = this.Tokens[Pos + 1];
        return true;
    }

    public bool TryPeek(out Token token)
    {
        token = default;

        if (Eof)
        {
            return false;
        }

        token = this.Tokens[Pos];
        return true;
    }

    public bool IsNextOfType(TokenType type)
    {
        return TryPeek(out Token token) && token.Type == type;
    }
}