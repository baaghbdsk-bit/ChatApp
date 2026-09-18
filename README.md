# ChatApp - Learning Edition

A free learning project for understanding application architecture, Docker, CI/CD,
and deployment by building a working chat application.

## Current Architecture

```text
Browser
  -> Gradio frontend container (Python, port 7860)
      -> Docker network
          -> ASP.NET Core API container (port 5000)
              -> in-memory repositories
```

The current frontend is Gradio for fast experimentation. Angular is planned for
a later release. Data is currently stored in memory, so it is intentionally lost
when the backend container is removed or restarted.

## Run With Docker Compose

From the repository root:

```powershell
docker compose up --build
```

Open the frontend at <http://localhost:7860>.

The API is available at <http://localhost:5000> and its health endpoint is:

<http://localhost:5000/health>

To stop the application:

```powershell
docker compose down
```

## Run the Published Images Locally

This simulates a staging server. It pulls the images from Docker Hub instead of
building from local source code:

```powershell
docker compose -f docker-compose.staging.yml pull
docker compose -f docker-compose.staging.yml up -d
```

Open the staging-like frontend at <http://localhost:7862>. The API is available
at <http://localhost:5002/health>.

Stop it with:

```powershell
docker compose -f docker-compose.staging.yml down
```

## Run Without Docker

Start the API:

```powershell
dotnet run --project backend/ChatApp.API/ChatApp.API.csproj
```

Start the frontend in another terminal:

```powershell
cd frontend
pip install -r requirements.txt
python app.py
```

## Repository Structure

```text
ChatApp/
├── backend/
│   ├── ChatApp.API/            # Controllers, SignalR hub, application startup
│   ├── ChatApp.Core/           # Models, DTOs, and interfaces
│   └── ChatApp.Infrastructure/ # In-memory repositories and authentication
├── frontend/
│   ├── app.py                  # Gradio frontend
│   ├── Dockerfile
│   └── requirements.txt
├── dockerfiles/
│   └── chatapp-api.dockerfile  # Multi-stage .NET Docker build
├── docker-compose.yml          # Local multi-container deployment
├── docker-compose.staging.yml  # Runs images pulled from Docker Hub
└── .github/workflows/build.yml # Automatic Docker build validation
```

## CI/CD

GitHub Actions runs when code is pushed to `main` or a pull request targets
`main`. Pull requests build both Docker images. Pushes to `main` build and
publish both images to Docker Hub. Deployment is still manual.

## Kubernetes Status

Kubernetes is **not used yet**. The application currently runs with Docker
Compose. Kubernetes will be introduced later on a local free cluster such as
Kind or Minikube, after the Docker deployment model is fully understood.

The planned Kubernetes concepts are:

- Pod: runs one or more application containers
- Deployment: manages replicated application Pods
- Service: provides stable networking to Pods
- ConfigMap and Secret: provide runtime configuration

## Planned Learning Path

1. Understand the current Docker and Compose deployment.
2. Add CI/CD validation with GitHub Actions.
3. Deploy the current version using a free-first approach.
4. Add PostgreSQL and persistent volumes in version 2.
5. Replace the Gradio frontend with Angular in a later release.
6. Deploy the services to a local Kubernetes cluster.

This project is for learning and is not yet production-grade authentication or
persistent storage.
