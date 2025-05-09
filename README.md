# Deflektor

**Smart Email Support System with C#, Microsoft Graph API, Semantic Kernel, and OpenAI**

Deflektor is a reference implementation of an intelligent email support system that combines Microsoft technologies to automatically process and respond to support requests. Built with C#, Azure Functions, and OpenAI, this project showcases how to connect Microsoft Graph APIs with AI to deliver smart, scalable automation for your inbox.

> ⚠️ This project is intended for **demo and educational purposes only**. It demonstrates architectural patterns and real-world usage of APIs, cloud functions, and AI. Use with caution in production environments.

---

## ✨ Features

* Triggered on new support emails using **Azure Functions** and **Graph Webhooks**
* Retrieves email content and metadata using **Microsoft Graph API**
* Uses **Semantic Kernel** with support for **Small Language Models (SLMs)** for fast and efficient inference
* Leverages **OpenAI** to understand user intent and generate relevant responses
* Sends automatic replies directly from your support mailbox
* Optional: includes support for **custom documentation retrieval** using embeddings (RAG pattern)

---

## 🔧 Tech Stack

| Component             | Technology                  |
| --------------------- | --------------------------- |
| Backend               | C# (.NET 8+)                |
| Hosting               | Azure Functions             |
| AI Engine             | Azure OpenAI or OpenAI API  |
| Email Access          | Microsoft Graph API         |
| Knowledge Integration | Semantic Kernel (RAG-ready) |

---

## 📸 Architecture

1. New email arrives in the support inbox
2. Azure Function triggers via webhook
3. Graph API fetches the email content
4. Semantic Kernel + OpenAI (or a local model with LM Studio) parse the question
5. Reply is generated and sent back automatically

---

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/kasuken/deflektor.git
cd deflektor
```

### 2. Create Azure App Registration

To access the mailbox via Microsoft Graph API:

* Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
* Register a new app
* Add **Application permissions**:

  * `Mail.Read`
  * `Mail.Read.All`
  * `Mail.Send`
    
* Click **Grant admin consent**
* Copy the **Client ID**, **Tenant ID**, and **Client Secret**

### 3. Configure `local.settings.json`

Inside the Azure Functions project:

```json
{
  "Values": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

### 4. Expose Your Localhost Using ngrok

To receive notifications from Microsoft Graph, you need a public HTTPS endpoint:

Install and run [ngrok](https://ngrok.com):

```bash
ngrok http 7071
```

This will give you a public HTTPS URL like:

```
https://94d9-188-155-252-235.ngrok-free.app
```

Update your subscription to use this URL.

### 5. Create a Webhook Subscription to Graph API

You can register a subscription to watch for new emails:

```json
{
   "changeType": "created",
   "notificationUrl": "https://94d9-188-155-252-235.ngrok-free.app/api/ElaborateEmail",
   "resource": "users/30ada592-0c39-4303-9875-afd138b29bbd/mailFolders('inbox')/messages",
   "expirationDateTime": "2025-05-10T18:23:45.9356913Z",
   "clientState": "secretClientValue",
   "latestSupportedTlsVersion": "v1_2"
}
```

Use [Graph Explorer](https://developer.microsoft.com/en-us/graph/graph-explorer) or Postman to register this subscription.

---

## 🧠 How It Works

* **Azure Function** polls the inbox on a timer or reacts to a webhook
* **Microsoft Graph API** pulls email content and sender details
* **Semantic Kernel** handles orchestration and memory/context
* **OpenAI** (or your preferred model) understands and replies
* The response is sent using **Graph API's `sendMail`**

---

## 📚 Demo Use Case

This project is great for:

* Exploring AI-powered email automation
* Demonstrating real-world Microsoft Graph + AI scenarios
* Learning how to embed domain knowledge in LLM prompts using Semantic Kernel
* Showcasing integration between cloud functions, APIs, and AI

---

## ⭐️ Star This Project

If you find this project helpful or inspiring, please consider giving it a ⭐️ on GitHub to help others discover it!
