using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable CS0618,SKEXP0001, SKEXP0010, SKEXP0020,SKEXP0110
public class AgentGroupChatFactory
{
    public static AgentGroupChat CreateAgentGroupChat(
        ChatCompletionAgent sqlGeneratorAgent,
        ChatCompletionAgent sqlExecutorAgent,
        ChatCompletionAgent sqlSummarizerAgent,
        ChatCompletionAgent sqlReviewerAgent,
        Kernel kernel,
        KernelFunction selectionFunction,
        KernelFunction terminationFunction,
        ChatHistoryTruncationReducer historyReducer,
        string terminationToken)
    {
        return new AgentGroupChat(sqlGeneratorAgent, sqlExecutorAgent, sqlSummarizerAgent, sqlReviewerAgent)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy =
                    new KernelFunctionSelectionStrategy(selectionFunction, kernel)
                    {
                        // Always start with the editor agent.
                        InitialAgent = sqlGeneratorAgent,
                        // Save tokens by only including the final response
                        HistoryReducer = historyReducer,
                        // The prompt variable name for the history argument.
                        HistoryVariableName = "lastmessage",
                        // Returns the entire result value as a string.
                        ResultParser = (result) => result.GetValue<string>() ?? sqlGeneratorAgent.Name
                        
                    },
                TerminationStrategy =
                    new KernelFunctionTerminationStrategy(terminationFunction, kernel)
                    {
                        // Only evaluate for editor's response
                        Agents = new[] { sqlReviewerAgent },
                        // Save tokens by only including the final response
                        HistoryReducer = historyReducer,
                        // The prompt variable name for the history argument.
                        HistoryVariableName = "lastmessage",
                        // Limit total number of turns
                        MaximumIterations = 5,
                        // Customer result parser to determine if the response is "yes"
                        ResultParser = (result) => result.GetValue<string>()?.Contains(terminationToken, StringComparison.OrdinalIgnoreCase) ?? false
                    }
            }
        };
    }
}
