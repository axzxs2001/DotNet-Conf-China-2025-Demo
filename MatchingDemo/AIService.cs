using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.ClientModel;
using System.Net.NetworkInformation;
using System.Text.Json;

#pragma warning disable
namespace MatchingDemo
{
    public interface IAIService
    {
    }
    public class AIService
    {
        private readonly string _endpoint;
        private readonly string _deploymentName;
        private readonly ApiKeyCredential _credential;
        public AIService()
        {
            var parmeters = File.ReadAllLines("C:/gpt/azure_key.txt");
            _endpoint = parmeters[1];
            _deploymentName = parmeters[0];
            _credential = new ApiKeyCredential(parmeters[2]);
        }


        public async Task<AIAgent> TranslationAsync()
        {
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();
            var translationInstructions = """
                在不改变原意的情况下，将用户提供的信息翻译成日语。
                """;
            var translationAgent = new ChatClientAgent(chatClient, instructions: translationInstructions, name: "TranslationAgent");
            var optimizeInstructions = """
                优化以下日语翻译，使其更加自然流畅
                """;
            var optimizeAgent = new ChatClientAgent(chatClient, instructions: optimizeInstructions, name: "OptimizeAgent");
            var workflow = AgentWorkflowBuilder.BuildSequential(translationAgent, optimizeAgent);
            return workflow.AsAgent(name: "TranslationAgent");
        }
        
        public async Task<AIAgent> FindParallelSchoolsAsync()
        {
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();
            var extractParallelSchoolsInstructions = """
                抽取出毕业院校。
                """;
            var extractParallelSchoolsAgent = new ChatClientAgent(chatClient, instructions: extractParallelSchoolsInstructions, name: "ExtractParallelSchoolsAgent");
            var findParallelSchoolsInstructions = """
                优化以下
                """;
            var findParallelSchoolsAgent = new ChatClientAgent(chatClient, instructions: findParallelSchoolsInstructions, name: "FindParallelSchoolsAgent");
            var workflow = AgentWorkflowBuilder.BuildSequential(extractParallelSchoolsAgent, findParallelSchoolsAgent);
            return workflow.AsAgent(name: "ParallelSchoolsAgent");
        }

        public async Task<AIAgent> FindParallelCompaniesAsync()
        {
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();
            var extractParallelCompaniesInstructions = """
                抽取出工作过的公司。
                """;
            var extractParallelCompaniesAgent = new ChatClientAgent(chatClient, instructions: extractParallelCompaniesInstructions, name: "ExtractParallelCompaniesAgent");
            var findParallelSchoolsInstructions = """
                优化以下
                """;
            var findParallelCompaniesAgent = new ChatClientAgent(chatClient, instructions: findParallelSchoolsInstructions, name: "FindParallelCompaniesAgent");
            var workflow = AgentWorkflowBuilder.BuildSequential(extractParallelCompaniesAgent, findParallelCompaniesAgent);
            return workflow.AsAgent(name: "ParallelCompaniesAgent");
        }

        public async Task RunAsync()
        {
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();

            // 创建代理
            var japaneseAgent = GetTranslationAgent("日语", chatClient);
            AIAgent vietnameseAgent = GetTranslationAgent("越南语", chatClient);
            AIAgent englishAgent = GetTranslationAgent("英语", chatClient);

            // 通过添加执行器并连接它们来构建工作流 
            //var workflow = new WorkflowBuilder(japaneseAgent)
            //    .AddEdge(japaneseAgent, vietnameseAgent)
            //    .AddEdge(vietnameseAgent, englishAgent)
            //    .Build();


            var workflow = AgentWorkflowBuilder.BuildSequential(from lang in (string[])["日语", "越南语", "英语"] select GetTranslationAgent(lang, chatClient));


            // 执行工作流
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, "你好呀，你来自那里!"));

            // 必须发送轮次令牌以触发代理。
            // 代理被包装为执行器。当它们接收到消息时,
            // 它们会缓存消息,并仅在接收到 TurnToken 时才开始处理。
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
            var id = "";
            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                if (evt is AgentRunUpdateEvent executorComplete)
                {
                    if (id == "" || executorComplete.ExecutorId != id)
                    {
                        if (id != "")
                        {
                            Console.WriteLine();
                        }
                        id = executorComplete.ExecutorId;
                        Console.Write($"{executorComplete.ExecutorId}: {executorComplete.Data}");
                        continue;
                    }
                    Console.Write($"{executorComplete.Data}");
                }
            }
        }

        /// <summary>
        /// 为指定的目标语言创建翻译代理。
        /// </summary>
        /// <param name="targetLanguage">翻译的目标语言</param>
        /// <param name="chatClient">代理使用的聊天客户端</param>
        /// <returns>为指定语言配置的 ChatClientAgent</returns>
        ChatClientAgent GetTranslationAgent(string targetLanguage, IChatClient chatClient) =>
           new(chatClient, $"你是一个翻译助理，可以用 {targetLanguage} 翻译用户提供的信息，直接给出翻译结果即可.");
    }
}
