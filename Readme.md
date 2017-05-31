# MaulingMonkey.Identify

MaulingMonkey's simplified .NET API for identifying users.

This library is written by a security amature and should probably not be used for anything actually requiring security.  At least not without having a security professional you trust reviewing it thoroughly.

Licensed under [Apache 2.0](LICENSE.txt).

# Example

```csharp
...
```

# TODO

- Basic project sanity
  - Publish dependency nugets
  - Nugetify static microserver
  - Exponential backoff for certs

- Security Review
  - Fuzz the hell out of everything
  - Get a proper code review from someone I trust
  - Get a proper code review from a security professional

- More unit tests
  - Invalid JWTs
    - Expired
    - Wrong audience
    - Wrong issuer
    - Invalid signature
    - Symmetricly signed JWT?
    - Unsigned JWT
  - "Valid" JWTs
    - ...vs cached result with expiration disabled or clock faked (injectable time?)
    - ...get real JWT and real certs and compare
  - Performance/sanity
    - Verify certs caching
    - Verify no request storms on certificate expiration

# Dirt Trail Map

Because hobbies don't rate a roadmap.

| Id/Auth Provider/API | Status                 |
| -------------------- | ---------------------- |
| Google               | Soon (tm)              |
| Twitter              | Not yet?               |
| Facebook             | Maybe if you pay me to |
|                      |                        |
| Github               | Not yet?               |
| Gitlab               | Not yet?               |
| Stack Overflow       | Not yet?               |
