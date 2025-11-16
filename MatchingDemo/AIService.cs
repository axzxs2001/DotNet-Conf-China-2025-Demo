using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using OpenAI.Chat;
using System.ClientModel;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;

#pragma warning disable
namespace MatchingDemo
{
    public interface IAIService
    {
        Task<string> PerfectTranslationAsync();
        Task<List<AgentResult>> CultureTranslationAsync(string cv);
        Task<string> ApplyingMotivationAsync();
        Task<string> OptimizePhotosAsync(string name, string prompt);
    }
    public class AIService : IAIService
    {
        private readonly string _endpoint;
        private readonly string _deploymentName;
        private readonly ApiKeyCredential _credential;
        private readonly string _googleApiKey;
        private readonly string _googleEndpoint;
        public AIService()
        {
            var parmeters = File.ReadAllLines("C:/gpt/azure_key.txt");
            _endpoint = parmeters[1];
            _deploymentName = parmeters[0];
            _credential = new ApiKeyCredential(parmeters[2]);
            _googleApiKey = File.ReadAllText("C:/gpt/googlecloudkey.txt");
            _googleEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image-preview:generateContent";
        }

        #region 翻译优化代理工作流
        /// <summary>
        /// 翻译优化代理工作流
        /// </summary>
        /// <returns></returns>
        public async Task<AIAgent> TranslationAsync()
        {
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();
            var translationInstructions = """
                **指令：**
                将我提供的中文简历内容翻译成日语简历。

                **要求：**
                1. 保持原意，不增删信息。
                2. 使用日本商务简历常见的专业表达方式。
                3. 适当调整语序以符合日语自然用法。
                4. 技术名词、岗位名称保持准确性。
                5. 统一使用敬体（です・ます）风格。

                **约束：**
                * 不进行任何内容美化、润色或扩写。
                * 不改变原简历结构（段落、项目符号等）。
                * 不添加主观评价。
                * 若原文含有模糊内容，不自行推断。
                """;
            var translationAgent = new ChatClientAgent(chatClient, instructions: translationInstructions, name: "TranslationAgent");
            var optimizeInstructions = """                
                请对我提供的日语文本进行优化，使其表达更加自然、流畅且符合日本母语者的书写习惯。

                **输出要求：**
                * 仅返回优化后的日语文本
                * 不改变原文含义
                * 不增删信息，不加入主观推断
                * 使用自然且正式的日语表达（敬体）
                * 若存在不自然的词语或语序，请进行适当调整                
                """;
            var optimizeAgent = new ChatClientAgent(chatClient, instructions: optimizeInstructions, name: "OptimizeAgent");
            var workflow = AgentWorkflowBuilder.BuildSequential(translationAgent, optimizeAgent);
            return workflow.AsAgent(name: "OptimizeAgent", description: "完成中文到日文的翻译优化");
        }
        /// <summary>
        /// 处理平行大学代理工作流 
        /// </summary>
        /// <returns></returns>
        async Task<AIAgent> FindParallelSchoolsAsync()
        {
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();
            var extractParallelSchoolsInstructions = """
                仔细分析用户提供的数据，抽取出用户毕业大学，如果是多个，请返回一个集合。
                """;
            var extractParallelSchoolsAgent = new ChatClientAgent(chatClient, instructions: extractParallelSchoolsInstructions, name: "ExtractParallelSchoolsAgent");
            var findParallelSchoolsInstructions = """
                请根据以下 4 个维度，为我指定的中国高校匹配 1 所相当的日本大学，不需要给出理由：
                1. **层次对应**：
                   * 985/双一流 ≈ 旧帝大、顶尖国立、早大、庆应
                   * 211/普通本科 ≈ 中上层国立、公立、优秀私立
                   * 高职 ≈ 日本短大、专门学校
                2. **学科优势匹配**：
                   工科（东工大、阪大）、医学（东大、京大）、农林（北大）、外语国际（上智）、艺术（东京艺大）等。
                3. **学校类型**：
                   中国公办 ≈ 日本国立/公立；民办 ≈ 私立。
                4. **规模与特色**：
                   综合/单科、国际化程度、科研实力等进行相似度判断。
                **输出要求：**
                * 列出匹配的日本大学，只列出匹配的日本大学
                * 如果是多所中国院大学，分别对应一所日本大学
                * 输出的大学名称使用日文全称
                * 返回格式为：[{"CN":"中国大学名","JP":"日本大学名"}]，要求只返回json
                """;
            var findParallelSchoolsAgent = new ChatClientAgent(chatClient, instructions: findParallelSchoolsInstructions, name: "FindParallelSchoolsAgent");
            var workflow = AgentWorkflowBuilder.BuildSequential(extractParallelSchoolsAgent, findParallelSchoolsAgent);
            return workflow.AsAgent(name: "FindParallelSchoolsAgent", description: "完成中日大学平行转换");
        }
        /// <summary>
        /// 处理平行公司代理工作流
        /// </summary>
        /// <returns></returns>
        async Task<AIAgent> FindParallelCompaniesAsync()
        {
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();
            var extractParallelCompaniesInstructions = """
                仔细分析用户提供的数据，抽取出用户工作过的公司名称，如果是多个，请返回一个集合。
                """;
            var extractParallelCompaniesAgent = new ChatClientAgent(chatClient, instructions: extractParallelCompaniesInstructions, name: "ExtractParallelCompaniesAgent");
            var findParallelSchoolsInstructions = """
                请根据以下 4 个维度，为我指定的中国公司匹配 1 家相当的日本公司，不需要并给出理由：
                1. **行业与主营业务对应**
                   例如：互联网、制造业、汽车、金融、家电、通信、半导体、食品、零售等。
                2. **公司规模与市场地位**
                   按收入规模、市占率、品牌影响力、大/中/小型企业进行对标。
                3. **公司性质匹配**
                   * 国企/央企 ≈ 日本大型综合集团／财阀系企业
                   * 民企 ≈ 日本民营大型或中型公司
                   * 科技创业公司 ≈ 日本创新型/独角兽企业
                4. **技术/产品/业务模式相似度**
                   如：平台型、制造型、研发驱动、消费品牌、电商模式等。
                **输出要求：**
                * 列出日本公司名称，只列出日本公司的名称
                * 如果是多个中国公司，分别对应一个日本公司
                * 输出的日本公司用日文名称
                * 返回格式为：[{"CN":"中国公司名","JP":"日本公司名"}]，要求只返回json
                """;
            var findParallelCompaniesAgent = new ChatClientAgent(chatClient, instructions: findParallelSchoolsInstructions, name: "FindParallelCompaniesAgent");
            var workflow = AgentWorkflowBuilder.BuildSequential(extractParallelCompaniesAgent, findParallelCompaniesAgent);
            return workflow.AsAgent(id: "FindParallelCompaniesAgent", name: "FindParallelCompaniesAgent", description: "完成中日公司平行转换");
        }

        public async Task<string> PerfectTranslationAsync()
        {
            var cv = File.ReadAllText("CV.md");
            var results = await CultureTranslationAsync(cv);
            var companiesMessage = results.FirstOrDefault(r => r.AgentName == "FindParallelCompaniesAgent");
            var schoolsMessage = results.FirstOrDefault(r => r.AgentName == "FindParallelSchoolsAgent");
            var newCV = results.FirstOrDefault(r => r.AgentName == "OptimizeAgent");
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();

            var instructions = $$"""              
                ##下面是简历：
                {{newCV?.Result}}中。

                ## 下面是公司对应关系：
                {{companiesMessage?.Result}}
                
                ## 下面是大学对应关系：
                {{schoolsMessage?.Result}}

                请根据上述公司对应关系和大学对应关系，在简历中找到对应的公司名称和大学名称，并在其后面增加对应的日本名称。
                **输出要求：**
                * 只在原来的公司名称和大学名称后面增加对应的日本名称，格式为：（日文名称 相当）
                * 保持简历的整体格式不变
                """;
            var agent = chatClient.CreateAIAgent(new ChatClientAgentOptions(name: " ", instructions: instructions));
            var response = await agent.RunAsync(instructions);

            return response.Text;
        }
        //public async Task<List<AgentResult>> CultureTranslationAsync(string cv)
        //{
        //    var translationAgent = await TranslationAsync();
        //    var parallelSchoolAgent = await FindParallelSchoolsAsync();
        //    var parallelCompaniesAgent = await FindParallelCompaniesAsync();
        //    var agents = new AIAgent[] { translationAgent, parallelSchoolAgent, parallelCompaniesAgent };
        //    var workflow = AgentWorkflowBuilder.BuildConcurrent(agents);

        //    var results = await RunWorkflowAsync(workflow, [new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, cv)]);
        //    List<AgentResult> outMessages = new();
        //    foreach (var item in results)
        //    {
        //        outMessages.Add(new AgentResult { AgentName = item.AuthorName, Result = item.Text });
        //    }
        //    return outMessages;

        //    static async Task<List<Microsoft.Extensions.AI.ChatMessage>> RunWorkflowAsync(Workflow workflow, List<Microsoft.Extensions.AI.ChatMessage> messages)
        //    {
        //        List<Microsoft.Extensions.AI.ChatMessage> outMessages = new();
        //        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
        //        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        //        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        //        {
        //            if (evt is WorkflowOutputEvent outputEvent)
        //            {
        //                outMessages = outputEvent.As<List<Microsoft.Extensions.AI.ChatMessage>>()!;       
        //            }
        //        }
        //        return outMessages;
        //    }          
        //}
        public async Task<List<AgentResult>> CultureTranslationAsync(string cv)
        {
            var translationAgent = await TranslationAsync();
            var parallelSchoolAgent = await FindParallelSchoolsAsync();
            var parallelCompaniesAgent = await FindParallelCompaniesAsync();
            var agents = new AIAgent[] { translationAgent, parallelSchoolAgent, parallelCompaniesAgent };
            var workflow = AgentWorkflowBuilder.BuildConcurrent(agents);
            var workflowAgent = workflow.AsAgent(id: "workflow-agent", name: "Workflow Agent");
            var workflowAgentThread = workflowAgent.GetNewThread();
            var response = await workflowAgent.RunAsync(cv, workflowAgentThread);
            List<AgentResult> outMessages = new();
            foreach (var item in response.Messages)
            {
                switch (item.AuthorName)
                {
                    case "FindParallelCompaniesAgent":
                        outMessages.Add(new AgentResult { AgentName = "FindParallelCompaniesAgent", Result = item.Text });
                        break;
                    case "FindParallelSchoolsAgent":
                        outMessages.Add(new AgentResult { AgentName = "FindParallelSchoolsAgent", Result = item.Text });
                        break;
                    case "OptimizeAgent":
                        outMessages.Add(new AgentResult { AgentName = "OptimizeAgent", Result = item.Text });
                        break;
                }
            }
            return outMessages;
        }
        public async Task<string> ApplyingMotivationAsync()
        {
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();
            var instructions = """
                请根据以下信息，用**自然、专业的日语**为我生成一份志望動機。
                
                要求：
                * 字数控制在 **300～400字** 左右
                * 结构包含：
                  1. 对该职位感兴趣的原因
                  2. 想加入该公司的理由
                  3. 入社后能做出的贡献
                * 语言要符合日本求职惯例，避免过度夸张
                * 风格保持诚恳、逻辑清晰、与经历高度匹配 
                * 结合自己的能力
                * 结合简历的经验说明能带来什么具体贡献。
                """;

            var japneseCV = await PerfectTranslationAsync();
            var japaneseJob = File.ReadAllText("job.md");
            var message = $$"""
                下面是简历：
                {{japneseCV}}
                ----------------
                下面是公司介绍和职位要求：
                {{japaneseJob}}
                """;
            var agent = new ChatClientAgent(chatClient, instructions: instructions, name: "MotivationAgent");
            var response = await agent.RunAsync(message);
            return response.Text;
        }
        #endregion

        #region 照片优化代理工作流
        async Task<string> DetermineGenderAsync(string name)
        {
            var chatClient = new AzureOpenAIClient(new Uri(_endpoint), _credential).GetChatClient(_deploymentName).AsIChatClient();
            var instructions = """
                对输入图片进行人物分析，并根据视觉特征推测人物的性别呈现。
                主要能力：
                检测图片中的人物
                分析面部与身体视觉特征
                输出“男性”或“女性”的推测及置信度
                在无法判断时返回“不确定”
                """;

            var agent = chatClient.CreateAIAgent(name: "DetermineGenderAgent", instructions: instructions);

            var cvBytes = await File.ReadAllBytesAsync($"wwwroot/{name}");
            Microsoft.Extensions.AI.ChatMessage message = new(ChatRole.User, [
                new TextContent("判断该人的性别是男性还是女性，只需要返回“男性”或“女性”。"),
                new DataContent( cvBytes, "image/jpeg")
                ]);

            var thread = agent.GetNewThread();
            var response = await agent.RunAsync(message, thread);
            return response.Text;
        }

        public async Task<string> OptimizePhotosAsync(string name, string prompt)
        {
            var sex = await DetermineGenderAsync(name);
            var cvBytes = await File.ReadAllBytesAsync($"wwwroot/{name}");
            string cvB64 = Convert.ToBase64String(cvBytes);
            var instruction = "";
            if (sex.Contains("女"))
            {
                instruction = $$"""
                基于用户提供的照片转换成符合日本求职简历照片要求的专业证件照，要求如下：
                1. 不要改变面部特征，保持与原照片相似度高。
                2. 比例：高分辨率，宽高比是3:4。
                3. 背景：无图案，无边框。
                4. 服装：深色正式西装外套 + 浅色衬衫／上衣（女士款）。
                5. 发型：整洁，前刘海不遮眼，避免过于鲜艳发色。
                6. 化妆／妆容：自然、清爽、不过度。
                7. 表情：轻微微笑，嘴唇闭合，给人明亮、专业印象。
                8. 配饰：避免夸张耳环、项链、帽子等干扰专业形象。
                9. 光线与构图：正面胸部以上视角，脸部清晰、光线均匀，无明显阴影或反光。     
                其他要求如下：
                {{prompt}}
                """;
            }else if (sex.Contains("男"))
            {
                instruction = $$"""
                基于用户提供的照片转换成符合日本求职简历照片要求的专业证件照，要求如下：
                1. 不要改变面部特征，保持与原照片相似度高。
                2. 比例：高分辨率，宽高比3:4（例如4 cm×3 cm或企业指定尺寸）。
                3. 背景：纯色、无图案、无边框。
                4. 服装：深色西装外套 + 浅色衬衫，领口扣上，必须佩戴领带。
                5. 发型：整洁、干净刮胡须（或胡须修整干净），避免蓬乱或过于时髦的造型。
                6. 妆容／仪表：男士建议保持自然清爽，无须浓妆，但整体形象须干净、整洁。
                7. 表情：面向镜头，肩膀平直，略带微笑或自然表情，避免大笑露齿。
                8. 配饰：避免显眼饰品（大耳环、帽子、显著项链等），尽量简洁。
                9. 光线与构图：正面胸部以上视角，脸部清晰、光线均匀、无明显阴影或反光。
                10. 整体氛围：看起来像商务／面试用职业照，而不是休闲或生活自拍。
                其他要求如下：
                {{prompt}}
                """;
            }
            else
            {
                return null;
            }
                var requestBody = new
                {
                    contents = new[]
                    {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/png",
                                    data = cvB64
                                }
                            },
                            new { text = instruction }
                        }
                    }
                }
                };

            var url = $"{_googleEndpoint}?key={_googleApiKey}";
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var json = JsonSerializer.Serialize(requestBody);
            using var resp = await http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            int imageIndex = 0;
            foreach (var cand in doc.RootElement.GetProperty("candidates").EnumerateArray())
            {
                foreach (var part in cand.GetProperty("content").GetProperty("parts").EnumerateArray())
                {
                    if (part.TryGetProperty("inlineData", out var inline))
                    {
                        var b64 = inline.GetProperty("data").GetString();
                        var bytes = Convert.FromBase64String(b64!);
                        var file = $"photos/NewPhotos/cv_{DateTime.Now.ToString("yyyyMMddHHmmss")}.png";
                        await File.WriteAllBytesAsync(Path.Combine("wwwroot", file), bytes);
                        Console.WriteLine($"Saved: {file}");
                        return file;
                    }
                }
            }
            return null;
        }
        #endregion
        #region 位置匹配
        public async Task<List<Job>> MatchJobsAsync(double latitude, double longitude, string keyword)
        {
            var sql = """
                SELECT job_title,place_name
                FROM jobs
                WHERE ST_DistanceSphere(
                        ST_MakePoint(longitude, latitude),
                        ST_MakePoint(139.7088135, 35.7283911)
                      ) <= 4500;
                
                """;


            return new List<Job>
            {
                new Job
                {
                    PlaceName = "东京",
                    JobTitle = "软件工程师",
                    Company = "东京科技公司",
                    Latitude = 35.6895,
                    Longitude = 139.6917
                },
                new Job
                {
                    PlaceName = "大阪",
                    JobTitle = "数据分析师",
                    Company = "大阪数据公司",
                    Latitude = 34.6937,
                    Longitude = 135.5023
                }
            };
        }
        #endregion
    }

    public class AgentResult
    {
        public string AgentName { get; set; }
        public string Result { get; set; }
    }


    public class Job
    {
        /// <summary>
        /// 地点名称
        /// </summary>
        public string PlaceName { get; set; }

        /// <summary>
        /// 职位名称
        /// </summary>
        public string JobTitle { get; set; }

        /// <summary>
        /// 公司名称
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 经度
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// 纬度
        /// </summary>
        public double Latitude { get; set; }
    }

}

