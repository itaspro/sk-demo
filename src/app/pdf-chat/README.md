# Pdf-Chat with Semantic-Kernal and Azure OpenAI Service
Since the release of OpenAI GPT APIs, there has been tons of `Chat with your documents` like projects coming out. Open-sourced or not, they are mostlly using an open-source framework: LangChain and written in python. Microsoft recentlly published their own SDK for working with LLMs: Semantic-Kernel. This is a real-time `chat to your pdf` appliction that explores integrating LLMs capablities into ASP.Net applications using Semantic Kernel and Azure OpenAI Service.

It consists of two parts:

## Backend Api 
  This is an ASP.NET 6 WebApi + SignalR written in C#. It communitates with front-end app with websocket and https.
    
  ### Settings
  There are two OpenAI models deployed to Azure OpenAI services.
  
  1. embedding model 
    'text-embedding-ada-002' 
  2. chat completion 
   'gpt-35-turbo (version 0301)'.

Beaware, Semantic-Kernel expects you give the deployment name, not `model` name itself. In the following below, I have deployment name of `itas-gpt-35` for "gpt-35-turbo". 
 
  ```
     "AzureOpenAI": {
        "EndPoint": "https://<your-organization-name>.openai.azure.com/",
        "Embedding": "text-embedding-ada-002",
        "TextCompletion": "itas-gpt-35", // gpt-35-turbo (version 0301),
        "ApiKey":  "<your_api_key>"
    }
  ```

  SignalR requires Cors enabled with explicted domain allowed. Make sure you also update 
  `AllowedCors` in the appsettings.

## Front App
The front-end is a simple Vuejs application.

### Config
Enviroment variable `VUE_APP_BASE_URL` need to be set to the url to backend api
The easiest way is to update it in package.json

```
  "scripts": {
    "serve": "set VUE_APP_BASE_URL=https://localhost:7266&vue-cli-service serve",
    "build": "set VUE_APP_BASE_URL=https://pdf-chat-semantic.azurewebsites.net&vue-cli-service build",
    "lint": "vue-cli-service lint"
  },
```

### Compiles and hot-reloads for development
```
yarn serve
```

### Compiles and minifies for production
```
yarn build
```

### Lints and fixes files
```
yarn lint
```
