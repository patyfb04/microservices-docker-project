Play Economy – Microservices Architecture
Play Economy is a distributed microservices system designed to simulate a virtual game economy. It demonstrates modern cloud‑native architectural patterns including service decomposition, event‑driven communication, orchestration‑based sagas, resilience engineering, and secure API boundaries.

📐 Architecture Overview
Play Economy is composed of several independently deployable services, each responsible for a specific domain:
1. SPA Frontend (React)
- A single‑page application built with React.
- Communicates with backend services through REST APIs.
- Handles user interaction, authentication flows, and real‑time UI updates.
2. Trading Service (Orchestrator)
- The central orchestrator of business workflows.
- Implements the Saga Pattern (Orchestration) to coordinate multi‑service operations such as:
- Purchasing items
- Updating inventory
- Validating catalog data
- Uses MassTransit to publish and consume events.
- Ensures consistency across services without distributed transactions.
3. Catalog Service
- Manages the list of items available in the economy.
- Exposes CRUD operations for item definitions.
- Publishes domain events such as:
- CatalogItemCreated
- CatalogItemUpdated
- CatalogItemDeleted
- Uses MongoDB for persistence.
4. Inventory Service
- Tracks player inventory and item quantities.
- Reacts to events from Trading and Catalog.
- Ensures eventual consistency with the orchestrator.
- Stores data in MongoDB for scalability and high throughput.
5. Identity Service
- Provides authentication and authorization.
- Issues JWT tokens for secure communication between:
- Frontend → API Gateway / Services
- Service → Service (machine‑to‑machine)
- Supports scopes and roles for fine‑grained access control.

🗄️ Data Layer – MongoDB
All domain services (Catalog, Inventory, Trading) use MongoDB as their primary data store.
Reasons for choosing MongoDB:
- Horizontal scalability
- Flexible document schema
- High write throughput
- Ideal for event‑driven microservices
- Native support for GUIDs and JSON‑like structures
Each service owns its own database to ensure loose coupling and bounded contexts.

🛰️ Communication – Saga Pattern (Orchestration)
Play Economy uses Orchestration‑based Sagas to manage distributed workflows.
Why Orchestration?
- Centralized workflow logic
- Clear visibility into process state
- Easier debugging
- Stronger control over compensating actions
How it works:
- Trading Service receives a command (e.g., “Purchase Item”).
- It orchestrates calls to:
- Catalog Service (validate item)
- Inventory Service (reserve or deduct items)
- If any step fails:
- Trading triggers compensating actions
- Ensures eventual consistency across services
MassTransit handles:
- Message routing
- Event publishing
- Saga state persistence

🛡️ Fault Resilience – Polly
To ensure reliability in a distributed environment, Play Economy uses Polly for:
Retries
- Automatically retry transient failures
- Helps absorb temporary network issues
Circuit Breakers
- Prevents cascading failures
- Stops calls to unhealthy services
- Allows time for recovery before retrying
This results in:
- Higher system stability
- Better user experience
- Protection against service overload

🔐 Security – JWT Authentication
Security is implemented using JWT Bearer Authentication.
Features:
- Access tokens issued by Identity Service
- Supports:
- User authentication (frontend)
- Service‑to‑service authentication (backend)
- Policies enforced via:
- Roles (e.g., Admin)
- Scopes (e.g., catalog.readaccess, catalog.writeaccess)
Benefits:
- Stateless authentication
- Easy horizontal scaling
- Clear separation of concerns

- 📦 Technologies Used
- React
- .NET 9 Microservices
- MassTransit + RabbitMQ
- MongoDB
- Polly (Retries, Circuit Breakers)
- JWT Bearer Tokens
- Docker
- Serilog




