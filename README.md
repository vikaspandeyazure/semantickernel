# Agent-Based SQL Chat with Semantic Kernel

This project demonstrates an agent-based chat application using Microsoft's Semantic Kernel, designed to interact with an Azure SQL database. It features multiple agents with specific roles, including SQL query generation, execution, summarization, and review, all coordinated by a central chat orchestration system.

## Features

* **Agent-Based Architecture:** Utilizes Semantic Kernel's agent capabilities to create specialized agents for different tasks.
* **SQL Interaction:** Generates and executes SQL queries against an Azure SQL database.
* **Chat History Management:** Maintains and manages conversation history.
* **Vector Store Integration:** Stores and retrieves chat context using a vector store for enhanced conversation memory.
* **Summarization and Review:** Summarizes SQL query results and allows for review and improvement suggestions.
* **Token Counting:** Tracks input and output tokens for cost estimation.
* **FAQ Retrieval:** Ability to search FAQ's from a vector database.

## Prerequisites

* [.NET SDK](https://dotnet.microsoft.com/en-us/download) (Version 7.0 or higher recommended)
* Azure OpenAI Service access with deployed models for chat completion and embeddings.
* Azure SQL Database with connection string.
* [.NET Env](https://www.nuget.org/packages/dotenv.net) NuGet package.

## Setup

1.  **Clone the Repository:**

    ```bash
    git clone [repository-url]
    cd AgentSqlChat
    ```

2.  **Install Dependencies:**

    ```bash
    dotnet restore
    ```

3.  **Environment Variables:**

    * Create a `.env` file in the project's root directory (same directory as `AgentSqlChat.csproj`).
    * Add the following environment variables, replacing the placeholder values with your actual Azure credentials and connection string:

        ```dotenv
        AZURE_SQLCONNECTIONSTRING="Server=yourserver.database.windows.net;Database=yourdatabase;User Id=youruserid;Password=yourpassword;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
        AZURE_OPENAI_ENDPOINT="[https://your-openai-resource.openai.azure.com/](https://www.google.com/search?q=https://your-openai-resource.openai.azure.com/)"
        AZURE_OPENAI_API_KEY="your-openai-api-key"
        AZURE_OPENAI_DEPLOYMENTNAME="gpt-35-turbo"
        AZURE_OPENAI_MODEL="gpt-35-turbo"
        AZURE_OPENAI_EMBEDDING_ENDPOINT="[https://your-openai-resource.openai.azure.com/](https://www.google.com/search?q=https://your-openai-resource.openai.azure.com/)"
        AZURE_OPENAI_EMBEDDING_API_KEY="your-openai-embedding-api-key"
        AZURE_OPENAI_EMBEDDING_DEPLOYMENTNAME="text-embedding-ada-002"
        AZURE_OPENAI_EMBEDDING_MODEL="text-embedding-ada-002"
        ```

    * **Important:** Ensure that the deployment names match the models you've deployed in your Azure OpenAI service.

4.  **Run the Application:**

    ```bash
    dotnet run
    ```

## Usage

* The application starts an interactive chat session in the console.
* Enter SQL-related questions or commands. For example:
    * `What are the total sales for each product category?`
    * `Show me the top 5 customers by order value.`
* The system will generate, execute, and summarize SQL queries, and the Reviewer Agent will provide feedback.
* Type `EXIT` to quit the application.
* Type `RESET` to clear the conversation history.
* To read a file into the chat, prefix the file path with `#`. For instance: `#C:/data/query_instructions.txt`. The contents of the file will be added to the chat as your input.

## Code Structure

* `Program.cs`: Main application logic, including agent setup, chat loop, and console interaction.
* `AgentGroupChatFactory.cs`: Creates and configures the `AgentGroupChat` with specialized agents.
* `SelectionFunctionFactory.cs`: Defines the agent selection and termination logic using Semantic Kernel functions.
* `VectorStorePlugin.cs`: Handles vector store operations for chat context and FAQ retrieval, using `IVectorStore` and `ITextEmbeddingGenerationService`.
* `SearchDatabasePluginNoLog.cs`: Contains functions for interacting with the Azure SQL database, executing SQL queries and retrieving results.

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues to improve this project.

## License

[MIT](LICENSE)

## Acknowledgments

* Microsoft Semantic Kernel team for providing powerful tools for agent-based applications.
* Azure OpenAI and Azure SQL Database teams for their services.