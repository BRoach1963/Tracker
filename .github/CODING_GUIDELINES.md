# Coding Guidelines

## 1. General Best Practices

### DRY (Don't Repeat Yourself)
Avoid duplicating code. Extract reusable logic into functions, modules, or classes.

### KISS (Keep It Simple, Stupid)
Aim for simple, clear solutions. Avoid unnecessary complexity.

### YAGNI (You Aren't Gonna Need It)
Build only the features you need right now, not things that might be needed in the future.

### SOLID Principles

| Principle | Description |
|-----------|-------------|
| **Single Responsibility** | Keep each class or module focused on a single responsibility |
| **Open/Closed** | Code should be open for extension but closed for modification |
| **Liskov Substitution** | Subclasses must be replaceable with their base classes |
| **Interface Segregation** | Prefer smaller, specific interfaces over large, general-purpose ones |
| **Dependency Inversion** | High-level modules should not depend on low-level modules, but on abstractions |

---

## 2. Code Quality

### Naming Conventions
- Use descriptive and consistent names for variables, methods, and classes
- Follow language-specific conventions (e.g., PascalCase for C# methods, camelCase for local variables)

### Commenting & Documentation
- Focus on explaining **why** something is done, not just what
- Write meaningful docstrings/XML comments for functions and methods
- Keep comments up-to-date with code changes

### Error Handling
- Handle errors gracefully with clear, specific exception messages
- Log errors with sufficient context for debugging
- Avoid exposing sensitive data in error messages

---

## 3. Code Structure & Organization

### Modularity
- Organize code into modular, cohesive files and components
- Break large files into smaller, logically grouped ones
- Keep related functionality together

### Separation of Concerns (SoC)
- Maintain clear boundaries between different layers (UI, logic, data)
- Use ViewModels for UI logic, Services for business logic, Repositories for data access

### Design Patterns
Use common design patterns where appropriate:
- **Factory** - Object creation
- **Singleton** - Single instance services
- **Repository** - Data access abstraction
- **Command** - Encapsulate actions (especially for WPF)
- **Observer/Event** - Loose coupling between components

---

## 4. Testing & Quality Assurance

### Unit Testing
- Write unit tests for core functionality
- Use TDD (Test Driven Development) when possible
- Aim for comprehensive test coverage on business logic
- Mock external dependencies

### Integration Testing
- Test component interactions
- Verify database operations work correctly

---

## 5. Performance & Optimization

### Profile & Optimize
- Focus on optimizing bottlenecks identified through profiling
- Don't prematurely optimize - address performance issues as they arise
- Measure before and after optimization

### Scalability
- Design with scalability in mind
- Use asynchronous processing (`async`/`await`) to avoid blocking
- Use background jobs for long-running tasks
- Consider caching for frequently accessed data

---

## 6. Security

### Input Validation & Sanitization
- Validate and sanitize all user input
- Prevent injection attacks (SQL injection, XSS)
- Use parameterized queries for database operations

### Authentication & Authorization
- Use secure authentication methods (hashed passwords with salt)
- Implement proper authorization checks
- Follow principle of least privilege

### Data Protection
- Store sensitive information securely
- Use encryption for sensitive data at rest and in transit
- Never log sensitive data (passwords, tokens, PII)
- Comply with privacy regulations (GDPR, etc.)

---

## 7. Architecture Guidelines

### Microservices vs. Monolithic
- Choose architecture based on project size and complexity
- For microservices: Keep services small, independent, with well-defined APIs
- For monolithic: Maintain clear internal boundaries

### Event-Driven Architecture (EDA)
- Use event-driven patterns for loose coupling
- Leverage messaging for asynchronous operations
- Use events for cross-component communication (e.g., `DataMessenger`)

### APIs & Communication
- Design clear, versioned APIs
- Use RESTful principles for web services
- Document APIs thoroughly
- Handle API errors consistently

---

## Project-Specific Guidelines

### WPF/MVVM
- Keep Views (XAML) free of business logic
- Use Commands for user actions
- Implement `INotifyPropertyChanged` for data binding
- Use dependency injection for services

### Database
- Use migrations for schema changes
- Include appropriate indexes
- Use transactions for multi-step operations
- Handle connection failures gracefully

### Logging
- Use structured logging with appropriate levels (Debug, Info, Warn, Error)
- Include correlation IDs for tracing
- Log entry/exit points of important operations
