# 🚀 Windows Setup Guide

This guide helps you set up and run the ChatApp learning project on Windows.

## Prerequisites

### 1. .NET 9.0 SDK
Check if installed:
```powershell
dotnet --version
```

If not, install from: https://dotnet.microsoft.com/download

### 2. Python 3.11+
Check if installed:
```powershell
python --version
```

If not, install from: https://www.python.org/downloads/

### 3. Docker Desktop (Optional for container learning)
Download: https://www.docker.com/products/docker-desktop/

## Quick Start (Without Docker)

### Step 1: Start the .NET API

Open PowerShell and run:

```powershell
# Navigate to project
cd d:\Projects\ChatApp\src\ChatApp.API

# Run the API
dotnet run
```

You should see:
```
Now listening on: http://localhost:5000
Now listening on: http://localhost:5001
```

### Step 2: Install Gradio Frontend Dependencies

Open a **new** PowerShell window:

```powershell
# Navigate to frontend
cd d:\Projects\ChatApp\frontend

# Create virtual environment (optional but recommended)
python -m venv venv

# Activate virtual environment
.\venv\Scripts\Activate.ps1

# Install dependencies
pip install -r requirements.txt
```

### Step 3: Run the Gradio App

In the same PowerShell window (with venv activated):

```powershell
python app.py
```

You should see:
```
Running on local URL:  http://127.0.0.1:7860
```

### Step 4: Use the Chat App

1. Open your browser to `http://127.0.0.1:7860`
2. You'll see the Gradio chat interface
3. Create a conversation
4. Start chatting!

## Common Issues

### Issue: "Module not found" when running app.py

**Solution:**
```powershell
cd d:\Projects\ChatApp\frontend
.\venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### Issue: API connection failed errors

**Solution:**
- Make sure the .NET API is running on port 5000
- Check that `API_BASE_URL` in `frontend/app.py` is correct

### Issue: SignalR connection fails

**Solution:**
- SignalR is optional for learning
- The app will still work in "demo mode" without real-time updates
- Install the signalr-client-ai package:
```powershell
pip install signalr-client-ai
```

## Quick Commands Reference

```powershell
# Start .NET API
cd d:\Projects\ChatApp\src\ChatApp.API
dotnet run

# Activate Gradio venv
cd d:\Projects\ChatApp\frontend
.\venv\Scripts\Activate.ps1

# Run Gradio app
python app.py

# Deactivate venv
deactivate
```

## Next Steps

1. **Read** `frontend/app.py` - understand how Gradio works
2. **Modify** the UI colors/layout in `build_chat_interface()`
3. **Try Docker** - containerize both services
4. **Deploy** to Hugging Face Spaces (free hosting)

## Want to Use Docker Instead?

```powershell
cd d:\Projects\ChatApp
docker-compose up --build
```

This starts both .NET API (port 5000) and Gradio (port 7860).

---

**Note:** This is a learning project. The in-memory repositories reset on app restart - that's intentional for learning architecture without database complexity!
