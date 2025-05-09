using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Deflektor.Func
{
    public class DeflektorEngineService
    {
        public DeflektorEngineService()
        {

        }

        public async Task<TicketSupportResponse> ElaborateTicket(string emailBody)
        {
            var client = new HttpClient(new MyHttpMessageHandler());

            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion("fake-model-name", "fake-api-key", httpClient: client)
                .Build();

            var ticketPrompt = @"
                <Role>
                You are an expert IT Technical Support Specialist with extensive experience in hardware, software, networking, and cybersecurity. You possess exceptional communication skills and can explain complex technical concepts in simple terms.
                </Role>
	
                <Context>
                Users seek your help with various technical issues ranging from basic to complex problems. You must provide accurate, safe, and effective solutions while ensuring users feel supported and understood.
                </Context>
	
                <Instructions>
                1. Begin each interaction by gathering essential information about the technical issue
                2. Ask clarifying questions to understand the problem's scope and severity
                3. Provide step-by-step solutions in clear, jargon-free language
                4. Explain potential risks and necessary precautions
                5. Offer alternative solutions when applicable
                6. Include preventive measures to avoid future issues
                </Instructions>
	
                <Constraints>
                1. Never recommend actions that could compromise security or data integrity
                2. Always suggest backing up data before major changes
                3. Avoid highly technical jargon unless specifically requested
                4. Include warnings for potentially risky procedures
                5. Recommend professional help for hardware repairs or critical system issues
                </Constraints>
	
                <Output_Format>
                1. Problem Assessment: [Summarize the issue]
                2. Required Information: [List needed details]
                3. Solution Steps: [Numbered, clear instructions]
                4. Precautions: [Safety measures]
                5. Prevention Tips: [Future recommendations]
                </Output_Format>

                This is the email body from the user:
                {emailBody}
            ";

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.7,
                #pragma warning disable SKEXP0010
                ResponseFormat = typeof(TicketSupportResponse)
            };

            var ticketFunction = kernel.CreateFunctionFromPrompt(ticketPrompt, executionSettings);

            var arguments = new KernelArguments
            {
                { "emailBody", emailBody }
            };

            var response = await kernel.InvokeAsync(ticketFunction, arguments);
            var jsonResponse = response.GetValue<string>();

            
            var ticketResponse = JsonSerializer.Deserialize<TicketSupportResponse>(jsonResponse);

            return ticketResponse;
        }
    }
}
