using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;


#pragma warning disable CS0618,SKEXP0001, SKEXP0010, SKEXP0020,SKEXP0110
public class SelectionFunctionFactory
{

// public static KernelFunction CreateSelectionFunction(
//         string generatorName,
//         string executorName,
//         string summarizorName,
//         string reviewerName)
//     {
//         return AgentGroupChat.CreatePromptFunctionForStrategy(
//             $$$"""
//             Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
//             State only the name of the participant to take the next turn.
//             Never choose the participant named in the RESPONSE.

//             Choose only from these participants:
//             - {{{generatorName}}}
//             - {{{executorName}}}
//             - {{{summarizorName}}}
//             - {{{reviewerName}}}

//             Always follow these rules when choosing the next participant:
//             - After {{{generatorName}}} replies, it is {{{executorName}}}'s turn.
//             - After {{{executorName}}} replies, it is {{{summarizorName}}}'s turn.
//             - After {{{summarizorName}}} replies, it is {{{reviewerName}}}'s turn.
            
//             RESPONSE:
//             {{$lastmessage}}
//             """,
//             safeParameterNames: "lastmessage");
//     }

    public static KernelFunction CreateSelectionFunction(
        string generatorName,
        string executorName,
        string summarizorName,
        string reviewerName)
    {
        return AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$"""
            Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
            State only the name of the participant to take the next turn.
            Never choose the participant named in the RESPONSE.

            Choose only from these participants:
            - {{{generatorName}}}
            - {{{executorName}}}
            - {{{summarizorName}}}
            - {{{reviewerName}}}

            Always follow these rules when choosing the next participant and don't miss calling the participant:
            - After user input, it is {{{generatorName}}}'s turn.
            - If RESPONSE is by {{{generatorName}}} but SQL is not generated then it is {{{reviewerName}}}'s turn
            - If RESPONSE is by {{{generatorName}}} and SQL is generated then it is {{{executorName}}}'s turn
            - If RESPONSE is by {{{executorName}}}, it is {{{summarizorName}}}'s turn.
            - If RESPONSE is by {{{summarizorName}}}, it is {{{reviewerName}}}'s turn.
                        
            RESPONSE:
            {{$lastmessage}}
            """,
            safeParameterNames: "lastmessage");
    }

    //- If RESPONSE is by {{{reviewerName}}}, it is {{{generatorName}}}'s turn.
/*

return AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$"""
            Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
            State only the name of the participant to take the next turn.
            Never choose the participant named in the RESPONSE.

            Choose only from these participants:
            - {{{reviewerName}}}
            - {{{generatorName}}}
            - {{{executorName}}}
            - {{{summarizorName}}}

            Always follow these rules when choosing the next participant:
            - After user input, it is {{{reviewerName}}}'s turn.
            - After {{{reviewerName}}} replies, it is {{{generatorName}}}'s turn.
            - After {{{generatorName}}} replies, it is {{{executorName}}}'s turn to get approval from user to run.
            - After {{{executorName}}} replies, it is {{{summarizorName}}}'s turn.
            - After {{{summarizorName}}} replies, it is {{{reviewerName}}}'s turn to review the output.
            

            RESPONSE:
            {{$lastmessage}}
            """,
            safeParameterNames: "lastmessage");

*/

    // public static KernelFunction CreateTerminationFunction(string terminationToken)
    // {
    //     return AgentGroupChat.CreatePromptFunctionForStrategy(
    //         $$$"""
    //         Examine the RESPONSE and determine whether the content has been deemed satisfactory.
    //         If content is satisfactory, respond with a single word without explanation: {{{terminationToken}}}.
    //         If specific suggestions are being provided, it is not satisfactory.
    //         If no correction is suggested, it is satisfactory.

    //         RESPONSE:
    //         {{$lastmessage}}
    //         """,
    //         safeParameterNames: "lastmessage");
    // }

     public static KernelFunction CreateTerminationFunction(string reviewerName,string terminationToken)
    {
        return AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$"""
            Determine the RESPONSE if the {{{reviewerName}}} has satisfactory output.If so, respond with a single word without explanation: {{{terminationToken}}}.                    
            
            RESPONSE:
            {{$lastmessage}}
            """,
            safeParameterNames: "lastmessage");
    }
}
