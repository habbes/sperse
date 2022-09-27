# Sperse

https://user-images.githubusercontent.com/8460169/192595859-04756f0f-1c1d-42a4-aeb9-460edb205f38.mp4

This is an experimental system for **interactive**, **reactive**, **remote**, **distributed** computing.

This is an experimental project attempting to implement a vague idea I've had for a while now. I'm building this project out of curiosity, exploration and fun. It's not trying to solve any particular problem or address any use case. **It may or may not be useful**.

Here's what I have in mind so far (subject to change as the project progresses):

The system consists of one or more client environments and one or more remote servers. A client is an application that provides a REPL-like interface to the user. The user uses this REPL to type and execute code expressions similar to any REPL like Python, Node, etc. This is the **interactive** part of the system.

The expressions the user enters can either be execute locally on the client by its embedded interpreter or can be executed remotely on the server. The client's engine provides built-in facilities for allowing the user to specify whether an expression should execute locally or remotely. If the instructs the client execute the expression remotely, then the system will transmit the code and inputs to the remote server behinds the scene. The remote server will execute the expression and return the results. This is the **remote** part of the system.

The user can use the results of a remote execution to compute new expressions before they have arrived. The client's engine will automatically keep track of all pending results and all expressions that depend on pending results. When these results are ready, the engine will propagate these values to all the variables and expressions that depend on them. This is the **reactive** part of the system.

There might be multiple remote servers. The client can send expression to a particular server based on some constraints. For example, if the expression reads from a database, it will be sent to a server that can connect to the database. The client can also distribute work to different servers, for example when computing the sum of a large set of numbers. On server can handle half the input, and another server the other half. Then the client can transparently merge the results. This is the **distributed** part of the system.

# Tentative road-map for the proof-of-concept

The PoC is meant to be quick implementation that explores the feasibility of the idea and demonstrates its key concepts practically on a reduce scope.

- [x] Create basic interpreter supporting variable assignment and simple arithmetic expressions (or just +)
- [x] Simulate remote execution and reactive value propagation by delaying execution of expressions with `remote` keyword
- [x] Create server app that can execute expressions. gRPC for client-server communication
- [x] Implement basic code and dependency serialization
- [x] Send remote expressions to server for execution, propagate result values to pending variables on the client on response
- [ ] Add support for functions:
  - [x] code blocks
  - [x] function calls
  - [x] function definitions
  - [ ] function calls with pending dependencies
  - [ ] function defs and code blocks with pending dependencies? (probably not)
- [ ] "pin" function to a specific remote server
- [ ] Add support for more operations? (loops, conditionals, arrays, etc.)
- [x] Connection to multiple servers?
- [ ] Split computation across different servers (e.g. map-reduce like operations)
