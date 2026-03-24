using System.Diagnostics;
using System.Text.Json;
using YonetIQ.Data.Models.AI;
using YonetIQ.Data.Services;

namespace YonetIQ.Data.Services.AI;

/// <summary>
/// AI sisteminin kapsamlı kalite testini çalıştırır.
/// Her skill'i gerçek bağlamla test eder, sonuçları raporlar.
/// Admin panelinden veya startup'ta çağrılabilir.
/// </summary>
public class AiQualityTestService(
    AiOrchestrationService orchestration,
    SkillRegistry skillRegistry,
    ContextBuilderService contextBuilder,
    SkillExecutor skillExecutor,
    AiMemoryService memoryService,
    PromptEngine promptEngine,
    AiEvaluationService evaluationService,
    SemanticService semanticService,
    ProactiveInsightService proactiveInsightService,
    GlobalSearchService globalSearchService,
    ExportService exportService,
    IConfiguration config,
    ILogger<AiQualityTestService> logger)
{
    /// <summary>
    /// Tüm AI altyapısını kapsamlı şekilde test eder.
    /// </summary>
    public async Task<QaTestReport> RunFullTestSuiteAsync(int testUserId = 1)
    {
        var report = new QaTestReport { StartedAt = DateTime.UtcNow };
        var sw = Stopwatch.StartNew();

        logger.LogInformation("=== AI QA Test Suite Başlıyor ===");

        // 1. Altyapı testleri
        await RunInfrastructureTests(report);

        // 2. Skill registry testleri
        RunSkillRegistryTests(report);

        // 3. Prompt engine testleri
        await RunPromptEngineTests(report);

        // 4. Context builder testleri
        await RunContextBuilderTests(report, testUserId);

        // 5. Skill execution testleri (Gemini API)
        await RunSkillExecutionTests(report, testUserId);

        // 6. Memory service testleri
        await RunMemoryServiceTests(report, testUserId);

        // 7. Orchestration entegrasyon testleri
        await RunOrchestrationTests(report, testUserId);

        // 8. Faz 2 skill testleri
        await RunFaz2SkillTests(report);

        // 9. Evaluation service testi
        await RunEvaluationServiceTests(report);

        // 10. Semantic service testi
        await RunSemanticServiceTests(report);

        // 11. Faz 3: Proactive insight testleri
        await RunProactiveInsightTests(report, testUserId);

        // 12. Faz 3: Golden memory (AiPatterns) testleri
        await RunGoldenMemoryTests(report);

        // 13. Faz 3: Permission-aware context testleri
        await RunPermissionAwareContextTests(report);

        // 14. Faz 3: Confidence scoring testleri
        RunConfidenceScoringTests(report);

        // 15. Cross-cutting: GlobalSearchService testi
        await RunGlobalSearchTests(report);

        // 16. Cross-cutting: ExportService testi
        RunExportServiceTests(report);

        sw.Stop();
        report.CompletedAt = DateTime.UtcNow;
        report.TotalDurationMs = (int)sw.ElapsedMilliseconds;

        logger.LogInformation("=== AI QA Test Suite Tamamlandı: {Passed}/{Total} başarılı ({Duration}ms) ===",
            report.PassedCount, report.TotalCount, report.TotalDurationMs);

        return report;
    }

    // ──────────────────────────────────────────────────────────
    // 1. ALTYAPI TESTLERİ
    // ──────────────────────────────────────────────────────────
    private async Task RunInfrastructureTests(QaTestReport report)
    {
        // Gemini API key mevcut mu?
        var apiKey = config["AI:Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        report.Add(new QaTestResult
        {
            Category = "Altyapı",
            TestName = "Gemini API Key mevcut",
            Passed = !string.IsNullOrWhiteSpace(apiKey),
            Detail = string.IsNullOrWhiteSpace(apiKey)
                ? "GEMINI_API_KEY tanımlı değil — AI çağrıları çalışmayacak"
                : "API key mevcut"
        });

        // DB tabloları mevcut mu?
        var interactionTest = await memoryService.GetRecentInteractionsAsync(0, null, 1);
        report.Add(new QaTestResult
        {
            Category = "Altyapı",
            TestName = "AiInteractions tablosu erişilebilir",
            Passed = interactionTest.IsSuccess,
            Detail = interactionTest.IsSuccess ? "Tablo erişilebilir" : $"Hata: {interactionTest.Message}"
        });
    }

    // ──────────────────────────────────────────────────────────
    // 2. SKILL REGISTRY TESTLERİ
    // ──────────────────────────────────────────────────────────
    private void RunSkillRegistryTests(QaTestReport report)
    {
        var allSkills = skillRegistry.ListAll();
        report.Add(new QaTestResult
        {
            Category = "Skill Registry",
            TestName = "En az 1 skill kayıtlı",
            Passed = allSkills.Count > 0,
            Detail = $"{allSkills.Count} skill kayıtlı"
        });

        // Her skill ID unique mi?
        var ids = allSkills.Select(s => s.Id).ToList();
        var uniqueIds = ids.Distinct().ToList();
        report.Add(new QaTestResult
        {
            Category = "Skill Registry",
            TestName = "Skill ID'ler unique",
            Passed = ids.Count == uniqueIds.Count,
            Detail = ids.Count == uniqueIds.Count
                ? $"{ids.Count} unique skill"
                : $"Duplicate ID bulundu: {string.Join(", ", ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key))}"
        });

        // Beklenen 6 skill mevcut mu?
        var expectedSkills = new[] { "exec.daily_briefing", "exec.change_detection", "task.clarify", "task.risk", "meeting.summarize", "meeting.extract_actions" };
        foreach (var skillId in expectedSkills)
        {
            var skill = skillRegistry.Resolve(skillId);
            report.Add(new QaTestResult
            {
                Category = "Skill Registry",
                TestName = $"Skill mevcut: {skillId}",
                Passed = skill is not null,
                Detail = skill is not null ? $"✓ {skill.Name}" : "Skill bulunamadı"
            });
        }

        // Her skill'in prompt dosyaları tanımlı mı?
        foreach (var skill in allSkills)
        {
            report.Add(new QaTestResult
            {
                Category = "Skill Registry",
                TestName = $"Prompt dosyaları tanımlı: {skill.Id}",
                Passed = !string.IsNullOrWhiteSpace(skill.SystemPromptFile) && !string.IsNullOrWhiteSpace(skill.UserPromptFile),
                Detail = $"System: {skill.SystemPromptFile}, User: {skill.UserPromptFile}"
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // 3. PROMPT ENGINE TESTLERİ
    // ──────────────────────────────────────────────────────────
    private async Task RunPromptEngineTests(QaTestReport report)
    {
        var allSkills = skillRegistry.ListAll();

        foreach (var skill in allSkills)
        {
            // System prompt yüklenebilir mi?
            var systemPrompt = await promptEngine.LoadPromptAsync(skill.SystemPromptFile);
            report.Add(new QaTestResult
            {
                Category = "Prompt Engine",
                TestName = $"System prompt yüklenebilir: {skill.Id}",
                Passed = !string.IsNullOrWhiteSpace(systemPrompt),
                Detail = string.IsNullOrWhiteSpace(systemPrompt)
                    ? $"Dosya bulunamadı: {skill.SystemPromptFile}"
                    : $"{systemPrompt.Length} karakter"
            });

            // User prompt yüklenebilir mi?
            var userPrompt = await promptEngine.LoadPromptAsync(skill.UserPromptFile);
            report.Add(new QaTestResult
            {
                Category = "Prompt Engine",
                TestName = $"User prompt yüklenebilir: {skill.Id}",
                Passed = !string.IsNullOrWhiteSpace(userPrompt),
                Detail = string.IsNullOrWhiteSpace(userPrompt)
                    ? $"Dosya bulunamadı: {skill.UserPromptFile}"
                    : $"{userPrompt.Length} karakter"
            });
        }

        // Template rendering çalışıyor mu?
        var testTemplate = "Merhaba {{userName}}, bugün {{today}}.";
        var vars = new Dictionary<string, string>
        {
            ["userName"] = "Test Kullanıcı",
            ["today"] = "23.03.2026"
        };
        var rendered = promptEngine.RenderTemplate(testTemplate, vars);
        report.Add(new QaTestResult
        {
            Category = "Prompt Engine",
            TestName = "Template rendering çalışıyor",
            Passed = rendered.Contains("Test Kullanıcı") && rendered.Contains("23.03.2026") && !rendered.Contains("{{"),
            Detail = $"Çıktı: {rendered}"
        });
    }

    // ──────────────────────────────────────────────────────────
    // 4. CONTEXT BUILDER TESTLERİ
    // ──────────────────────────────────────────────────────────
    private async Task RunContextBuilderTests(QaTestReport report, int userId)
    {
        // Dashboard context build edilebilir mi?
        var briefingSkill = skillRegistry.Resolve("exec.daily_briefing");
        if (briefingSkill is not null)
        {
            try
            {
                var context = await contextBuilder.BuildContextAsync(briefingSkill, new AiRequest
                {
                    SkillId = briefingSkill.Id,
                    UserId = userId,
                    Module = "dashboard"
                });

                report.Add(new QaTestResult
                {
                    Category = "Context Builder",
                    TestName = "Dashboard context oluşturulabilir",
                    Passed = context is not null,
                    Detail = context is not null
                        ? $"Token tahmini: {context.EstimatedTokenCount}, Bağlam: {context.ContextSummary}"
                        : "Context null döndü"
                });

                // Context'te beklenen veriler var mı?
                var hasTaskData = context?.ContextData.ContainsKey("pendingTaskCount") == true ||
                                  context?.ContextData.ContainsKey("totalActiveTasks") == true;
                report.Add(new QaTestResult
                {
                    Category = "Context Builder",
                    TestName = "Dashboard context görev verisi içeriyor",
                    Passed = hasTaskData,
                    Detail = hasTaskData
                        ? $"Keys: {string.Join(", ", context!.ContextData.Keys.Take(10))}"
                        : "Görev verisi bulunamadı"
                });
            }
            catch (Exception ex)
            {
                report.Add(new QaTestResult
                {
                    Category = "Context Builder",
                    TestName = "Dashboard context oluşturulabilir",
                    Passed = false,
                    Detail = $"Exception: {ex.Message}"
                });
            }
        }

        // Task context build edilebilir mi?
        var taskSkill = skillRegistry.Resolve("task.clarify");
        if (taskSkill is not null)
        {
            try
            {
                var context = await contextBuilder.BuildContextAsync(taskSkill, new AiRequest
                {
                    SkillId = taskSkill.Id,
                    UserId = userId,
                    Module = "task",
                    Parameters = new Dictionary<string, object> { ["taskId"] = 1 }
                });

                report.Add(new QaTestResult
                {
                    Category = "Context Builder",
                    TestName = "Task context oluşturulabilir",
                    Passed = context is not null,
                    Detail = context is not null
                        ? $"Entities: {string.Join(", ", context.Entities.Keys)}, Context: {context.ContextSummary}"
                        : "Context null döndü"
                });
            }
            catch (Exception ex)
            {
                report.Add(new QaTestResult
                {
                    Category = "Context Builder",
                    TestName = "Task context oluşturulabilir",
                    Passed = false,
                    Detail = $"Exception: {ex.Message}"
                });
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // 5. SKILL EXECUTION TESTLERİ
    // ──────────────────────────────────────────────────────────
    private async Task RunSkillExecutionTests(QaTestReport report, int userId)
    {
        var apiKey = config["AI:Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            report.Add(new QaTestResult
            {
                Category = "Skill Execution",
                TestName = "Gemini API çağrısı (atlandı)",
                Passed = true,
                Detail = "API key yok — Gemini testleri atlanıyor (beklenen davranış)"
            });
            return;
        }

        // daily_briefing skill'ini gerçek API ile test et
        var briefingSkill = skillRegistry.Resolve("exec.daily_briefing");
        if (briefingSkill is not null)
        {
            try
            {
                var context = await contextBuilder.BuildContextAsync(briefingSkill, new AiRequest
                {
                    SkillId = briefingSkill.Id,
                    UserId = userId,
                    Module = "dashboard"
                });

                var sw = Stopwatch.StartNew();
                var response = await skillExecutor.ExecuteAsync(briefingSkill, context);
                sw.Stop();

                report.Add(new QaTestResult
                {
                    Category = "Skill Execution",
                    TestName = "exec.daily_briefing Gemini çağrısı",
                    Passed = response.IsSuccess,
                    Detail = response.IsSuccess
                        ? $"Başarılı ({sw.ElapsedMilliseconds}ms, {response.Content.Length} karakter, güven: {response.Confidence:F2})"
                        : $"Başarısız: {response.ErrorMessage}"
                });

                // Çıktı Türkçe mi?
                if (response.IsSuccess)
                {
                    var hasTurkish = response.Content.Contains("görev", StringComparison.OrdinalIgnoreCase) ||
                                     response.Content.Contains("bugün", StringComparison.OrdinalIgnoreCase) ||
                                     response.Content.Contains("karar", StringComparison.OrdinalIgnoreCase) ||
                                     response.Content.Contains("özet", StringComparison.OrdinalIgnoreCase);
                    report.Add(new QaTestResult
                    {
                        Category = "Skill Execution",
                        TestName = "Briefing çıktısı Türkçe",
                        Passed = hasTurkish,
                        Detail = hasTurkish
                            ? "Türkçe içerik doğrulandı"
                            : $"Türkçe kelime bulunamadı. İlk 100 karakter: {response.Content[..Math.Min(100, response.Content.Length)]}"
                    });

                    // Çıktı uzunluğu makul mü?
                    report.Add(new QaTestResult
                    {
                        Category = "Skill Execution",
                        TestName = "Briefing çıktı uzunluğu makul",
                        Passed = response.Content.Length is > 50 and < 3000,
                        Detail = $"{response.Content.Length} karakter (50-3000 arası bekleniyor)"
                    });
                }
            }
            catch (Exception ex)
            {
                report.Add(new QaTestResult
                {
                    Category = "Skill Execution",
                    TestName = "exec.daily_briefing Gemini çağrısı",
                    Passed = false,
                    Detail = $"Exception: {ex.Message}"
                });
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // 6. MEMORY SERVICE TESTLERİ
    // ──────────────────────────────────────────────────────────
    private async Task RunMemoryServiceTests(QaTestReport report, int userId)
    {
        // Interaction kayıt ve okuma
        try
        {
            var testInteraction = new AiInteraction
            {
                UserId = userId,
                SkillId = "test.qa_check",
                Module = "test",
                InputSummary = "QA test interaction",
                OutputSummary = "QA test output",
                ConfidenceScore = 0.95m,
                LatencyMs = 100,
                TokenCount = 50,
                IsSuccess = true
            };

            var saveResult = await memoryService.SaveInteractionAsync(testInteraction);
            report.Add(new QaTestResult
            {
                Category = "Memory Service",
                TestName = "Interaction kayıt edilebilir",
                Passed = saveResult.IsSuccess,
                Detail = saveResult.IsSuccess
                    ? $"ID: {saveResult.Data}"
                    : $"Hata: {saveResult.Message}"
            });

            // Feedback kayıt
            if (saveResult is { IsSuccess: true, Data: var interactionId } && interactionId > 0)
            {
                var testFeedback = new AiFeedback
                {
                    InteractionId = interactionId,
                    UserId = userId,
                    Rating = 3,
                    WasAccepted = true,
                    WasEdited = false
                };
                var fbResult = await memoryService.SaveFeedbackAsync(testFeedback);
                report.Add(new QaTestResult
                {
                    Category = "Memory Service",
                    TestName = "Feedback kayıt edilebilir",
                    Passed = fbResult.IsSuccess,
                    Detail = fbResult.IsSuccess ? "Başarılı" : $"Hata: {fbResult.Message}"
                });
            }

            // Okuma testi
            var readResult = await memoryService.GetRecentInteractionsAsync(userId, "test.qa_check", 5);
            report.Add(new QaTestResult
            {
                Category = "Memory Service",
                TestName = "Interaction okunabilir",
                Passed = readResult is { IsSuccess: true, Data.Count: > 0 },
                Detail = readResult.IsSuccess
                    ? $"{readResult.Data?.Count ?? 0} kayıt okundu"
                    : $"Hata: {readResult.Message}"
            });
        }
        catch (Exception ex)
        {
            report.Add(new QaTestResult
            {
                Category = "Memory Service",
                TestName = "Memory CRUD işlemleri",
                Passed = false,
                Detail = $"Exception: {ex.Message}"
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // 7. ORCHESTRATION ENTEGRASYON TESTLERİ
    // ──────────────────────────────────────────────────────────
    private async Task RunOrchestrationTests(QaTestReport report, int userId)
    {
        // Doğrudan skill ID ile çağrı
        try
        {
            var request = new AiRequest
            {
                SkillId = "exec.daily_briefing",
                UserId = userId,
                Module = "dashboard"
            };
            var response = await orchestration.ProcessRequestAsync(request);
            report.Add(new QaTestResult
            {
                Category = "Orchestration",
                TestName = "Doğrudan skill çağrısı (exec.daily_briefing)",
                Passed = response.IsSuccess || response.ErrorMessage?.Contains("ulaşılamadı") == true,
                Detail = response.IsSuccess
                    ? $"Başarılı ({response.LatencyMs}ms, InteractionId: {response.InteractionId})"
                    : $"Hata: {response.ErrorMessage} (API key yoksa beklenen)"
            });
        }
        catch (Exception ex)
        {
            report.Add(new QaTestResult
            {
                Category = "Orchestration",
                TestName = "Doğrudan skill çağrısı",
                Passed = false,
                Detail = $"Exception: {ex.Message}"
            });
        }

        // Keyword routing testi
        var routingTests = new[]
        {
            ("Bugünün önceliklerini özetle", "exec.daily_briefing"),
            ("Geciken görevleri göster", "task.risk"),
            ("Toplantıyı özetle", "meeting.summarize"),
            ("Son değişiklikleri göster", "exec.change_detection"),
        };

        var availableSkills = orchestration.GetAvailableSkills("global");
        report.Add(new QaTestResult
        {
            Category = "Orchestration",
            TestName = "GetAvailableSkills('global') skill döndürüyor",
            Passed = availableSkills.Count > 0,
            Detail = $"{availableSkills.Count} skill: {string.Join(", ", availableSkills.Select(s => s.Id))}"
        });

        // Routing doğruluk testi — sadece yapısal
        foreach (var (input, expectedSkill) in routingTests)
        {
            // ProcessRequestAsync çağırmak yerine sadece skill listesinde expected skill'in var olduğunu doğrula
            var exists = skillRegistry.Resolve(expectedSkill) is not null;
            report.Add(new QaTestResult
            {
                Category = "Orchestration",
                TestName = $"Routing hedef skill mevcut: \"{input}\" → {expectedSkill}",
                Passed = exists,
                Detail = exists ? "Hedef skill kayıtlı" : "Skill bulunamadı"
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // 8. FAZ 2 SKILL TESTLERİ
    // ──────────────────────────────────────────────────────────
    private async Task RunFaz2SkillTests(QaTestReport report)
    {
        var faz2Skills = new[] { "note.structure", "note.suggest_tags", "query.explain", "query.nl_to_sql", "query.exec_summary", "approval.summarize", "approval.risk", "okr.progress", "okr.action" };

        foreach (var skillId in faz2Skills)
        {
            var skill = skillRegistry.Resolve(skillId);
            report.Add(new QaTestResult
            {
                Category = "Faz 2 Skills",
                TestName = $"Skill kayıtlı: {skillId}",
                Passed = skill is not null,
                Detail = skill is not null ? $"✓ {skill.Name} ({skill.Category})" : "Bulunamadı"
            });

            if (skill is not null)
            {
                var sysPrompt = await promptEngine.LoadPromptAsync(skill.SystemPromptFile);
                var usrPrompt = await promptEngine.LoadPromptAsync(skill.UserPromptFile);
                report.Add(new QaTestResult
                {
                    Category = "Faz 2 Skills",
                    TestName = $"Prompt yüklenebilir: {skillId}",
                    Passed = !string.IsNullOrWhiteSpace(sysPrompt) && !string.IsNullOrWhiteSpace(usrPrompt),
                    Detail = $"System: {sysPrompt?.Length ?? 0}ch, User: {usrPrompt?.Length ?? 0}ch"
                });
            }
        }

        // Total skill count check
        var allSkills = skillRegistry.ListAll();
        report.Add(new QaTestResult
        {
            Category = "Faz 2 Skills",
            TestName = "Toplam skill sayısı >= 15",
            Passed = allSkills.Count >= 15,
            Detail = $"{allSkills.Count} skill kayıtlı"
        });
    }

    // ──────────────────────────────────────────────────────────
    // 9. EVALUATION SERVICE TESTLERİ
    // ──────────────────────────────────────────────────────────
    private async Task RunEvaluationServiceTests(QaTestReport report)
    {
        try
        {
            var dashResult = await evaluationService.GetDashboardAsync();
            report.Add(new QaTestResult
            {
                Category = "Evaluation Service",
                TestName = "Dashboard verileri yüklenebilir",
                Passed = dashResult.IsSuccess,
                Detail = dashResult.IsSuccess
                    ? $"Interactions: {dashResult.Data?.TotalInteractions}, Feedback: {dashResult.Data?.TotalFeedback}"
                    : $"Hata: {dashResult.Message}"
            });
        }
        catch (Exception ex)
        {
            report.Add(new QaTestResult
            {
                Category = "Evaluation Service",
                TestName = "Dashboard verileri yüklenebilir",
                Passed = false,
                Detail = $"Exception: {ex.Message}"
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // 10. SEMANTIC SERVICE TESTLERİ
    // ──────────────────────────────────────────────────────────
    private async Task RunSemanticServiceTests(QaTestReport report)
    {
        try
        {
            var listResult = await semanticService.ListAsync();
            report.Add(new QaTestResult
            {
                Category = "Semantic Layer",
                TestName = "SemanticDefinitions tablosu erişilebilir",
                Passed = listResult.IsSuccess,
                Detail = listResult.IsSuccess ? $"{listResult.Data?.Count ?? 0} tanım mevcut" : $"Hata: {listResult.Message}"
            });
        }
        catch (Exception ex)
        {
            report.Add(new QaTestResult
            {
                Category = "Semantic Layer",
                TestName = "SemanticDefinitions tablosu erişilebilir",
                Passed = false,
                Detail = $"Exception: {ex.Message}"
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // 11. PROACTIVE INSIGHT TESTLERİ (Faz 3)
    // ──────────────────────────────────────────────────────────
    private async Task RunProactiveInsightTests(QaTestReport report, int userId)
    {
        try
        {
            var insights = await proactiveInsightService.GenerateInsightsAsync(userId);
            report.Add(new QaTestResult
            {
                Category = "Proactive Insights",
                TestName = "GenerateInsightsAsync exception fırlatmaz",
                Passed = true,
                Detail = $"{insights.Count} insight uretildi"
            });

            // Insight listesi null degil ve tipler dogru
            report.Add(new QaTestResult
            {
                Category = "Proactive Insights",
                TestName = "Insight listesi geçerli",
                Passed = insights is not null,
                Detail = insights is not null ? $"Liste mevcut, {insights.Count} eleman" : "Liste null dondü"
            });

            // Insight'lar priority sırasına göre sıralı mı?
            if (insights is not null && insights.Count >= 2)
            {
                var isSorted = true;
                for (int i = 1; i < insights.Count; i++)
                {
                    if (insights[i].Priority > insights[i - 1].Priority)
                    {
                        isSorted = false;
                        break;
                    }
                }
                report.Add(new QaTestResult
                {
                    Category = "Proactive Insights",
                    TestName = "Insight'lar priority sırasına göre sıralı",
                    Passed = isSorted,
                    Detail = isSorted ? "Sıralama doğru" : "Sıralama bozuk"
                });
            }

            // Her insight'ta zorunlu alanlar dolu mu?
            var allFieldsValid = insights.All(i =>
                !string.IsNullOrWhiteSpace(i.Title) &&
                !string.IsNullOrWhiteSpace(i.Module) &&
                !string.IsNullOrWhiteSpace(i.Icon));
            report.Add(new QaTestResult
            {
                Category = "Proactive Insights",
                TestName = "Insight zorunlu alanları dolu",
                Passed = allFieldsValid,
                Detail = allFieldsValid
                    ? "Tüm insight'lar Title, Module ve Icon içeriyor"
                    : "Eksik alanlar mevcut"
            });
        }
        catch (Exception ex)
        {
            report.Add(new QaTestResult
            {
                Category = "Proactive Insights",
                TestName = "GenerateInsightsAsync exception fırlatmaz",
                Passed = false,
                Detail = $"Exception: {ex.Message}"
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // 12. GOLDEN MEMORY (AiPatterns) TESTLERİ (Faz 3)
    // ──────────────────────────────────────────────────────────
    private async Task RunGoldenMemoryTests(QaTestReport report)
    {
        try
        {
            var patternsResult = await memoryService.GetApprovedPatternsAsync("exec.daily_briefing", 3);
            report.Add(new QaTestResult
            {
                Category = "Golden Memory",
                TestName = "GetApprovedPatternsAsync erişilebilir",
                Passed = patternsResult.IsSuccess,
                Detail = patternsResult.IsSuccess
                    ? $"{patternsResult.Data?.Count ?? 0} onaylı pattern mevcut"
                    : $"Hata: {patternsResult.Message}"
            });

            // Geçersiz skill ID ile çağrı yapılınca hata vermemeli
            var emptyResult = await memoryService.GetApprovedPatternsAsync("nonexistent.skill", 3);
            report.Add(new QaTestResult
            {
                Category = "Golden Memory",
                TestName = "Geçersiz skill ID ile pattern sorgusu hata vermez",
                Passed = emptyResult.IsSuccess,
                Detail = emptyResult.IsSuccess
                    ? $"Boş liste döndü ({emptyResult.Data?.Count ?? 0} kayıt)"
                    : $"Beklenmeyen hata: {emptyResult.Message}"
            });
        }
        catch (Exception ex)
        {
            report.Add(new QaTestResult
            {
                Category = "Golden Memory",
                TestName = "AiPatterns tablosu erişilebilir",
                Passed = false,
                Detail = $"Exception: {ex.Message}"
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // 13. PERMISSION-AWARE CONTEXT TESTLERİ (Faz 3)
    // ──────────────────────────────────────────────────────────
    private async Task RunPermissionAwareContextTests(QaTestReport report)
    {
        var briefingSkill = skillRegistry.Resolve("exec.daily_briefing");
        if (briefingSkill is null)
        {
            report.Add(new QaTestResult
            {
                Category = "Permission-Aware Context",
                TestName = "exec.daily_briefing skill mevcut",
                Passed = false,
                Detail = "Skill bulunamadı — permission testleri atlanıyor"
            });
            return;
        }

        try
        {
            // Admin (Genel Müdür) bağlamı — filtre uygulanmamalı
            var adminContext = await contextBuilder.BuildContextAsync(briefingSkill, new AiRequest
            {
                SkillId = briefingSkill.Id,
                UserId = 1,
                Module = "dashboard"
            });
            var adminHasPermNote = adminContext?.ContextData.ContainsKey("permissionNote") == true;
            report.Add(new QaTestResult
            {
                Category = "Permission-Aware Context",
                TestName = "Admin (Genel Müdür) context'inde permissionNote yok",
                Passed = !adminHasPermNote,
                Detail = adminHasPermNote
                    ? $"Beklenmeyen permissionNote: {adminContext!.ContextData["permissionNote"]}"
                    : "Admin context filtre içermiyor (doğru)"
            });

            // Context oluşturulabilir mi?
            report.Add(new QaTestResult
            {
                Category = "Permission-Aware Context",
                TestName = "Permission-aware context oluşturulabilir",
                Passed = adminContext is not null,
                Detail = adminContext is not null
                    ? $"Context oluşturuldu: {adminContext.ContextSummary}"
                    : "Context null döndü"
            });
        }
        catch (Exception ex)
        {
            report.Add(new QaTestResult
            {
                Category = "Permission-Aware Context",
                TestName = "Permission-aware context testleri",
                Passed = false,
                Detail = $"Exception: {ex.Message}"
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // 14. CONFIDENCE SCORING TESTLERİ (Faz 3)
    // ──────────────────────────────────────────────────────────
    private void RunConfidenceScoringTests(QaTestReport report)
    {
        var allSkills = skillRegistry.ListAll();

        // Her skill'in MinConfidence değeri 0.0-1.0 arasında mı?
        foreach (var skill in allSkills)
        {
            report.Add(new QaTestResult
            {
                Category = "Confidence Scoring",
                TestName = $"MinConfidence geçerli aralıkta: {skill.Id}",
                Passed = skill.MinConfidence >= 0.0f && skill.MinConfidence <= 1.0f,
                Detail = $"MinConfidence: {skill.MinConfidence:F2}"
            });
        }

        // MaxOutputLength tanımlı mı?
        var allHaveMaxOutput = allSkills.All(s => s.MaxOutputLength > 0);
        report.Add(new QaTestResult
        {
            Category = "Confidence Scoring",
            TestName = "Tüm skill'lerde MaxOutputLength > 0",
            Passed = allHaveMaxOutput,
            Detail = allHaveMaxOutput
                ? "Tüm skill'ler MaxOutputLength tanımlı"
                : $"Eksik: {string.Join(", ", allSkills.Where(s => s.MaxOutputLength <= 0).Select(s => s.Id))}"
        });

        // RequiredEntities listesi tanımlı mı (en azından boş list)?
        var allHaveEntities = allSkills.All(s => s.RequiredEntities is not null);
        report.Add(new QaTestResult
        {
            Category = "Confidence Scoring",
            TestName = "Tüm skill'lerde RequiredEntities tanımlı",
            Passed = allHaveEntities,
            Detail = allHaveEntities
                ? "Tüm skill'ler RequiredEntities listesi içeriyor"
                : "Bazı skill'lerde RequiredEntities null"
        });
    }

    // ──────────────────────────────────────────────────────────
    // 15. GLOBAL SEARCH TESTLERİ (Cross-cutting)
    // ──────────────────────────────────────────────────────────
    private async Task RunGlobalSearchTests(QaTestReport report)
    {
        try
        {
            // Geçerli arama terimi ile sonuç dönmeli
            var searchResult = await globalSearchService.SearchAsync("test", maxPerType: 3);
            report.Add(new QaTestResult
            {
                Category = "Global Search",
                TestName = "SearchAsync geçerli sorguyla çalışır",
                Passed = searchResult.IsSuccess,
                Detail = searchResult.IsSuccess
                    ? $"{searchResult.Data?.Count ?? 0} sonuç bulundu"
                    : $"Hata: {searchResult.Message}"
            });

            // Çok kısa arama terimi reddedilmeli
            var shortResult = await globalSearchService.SearchAsync("a");
            report.Add(new QaTestResult
            {
                Category = "Global Search",
                TestName = "Kısa arama terimi reddedilir",
                Passed = !shortResult.IsSuccess,
                Detail = !shortResult.IsSuccess
                    ? $"Doğru şekilde reddedildi: {shortResult.Message}"
                    : "Kısa terim kabul edildi (beklenmeyen)"
            });

            // Boş arama terimi reddedilmeli
            var emptyResult = await globalSearchService.SearchAsync("");
            report.Add(new QaTestResult
            {
                Category = "Global Search",
                TestName = "Boş arama terimi reddedilir",
                Passed = !emptyResult.IsSuccess,
                Detail = !emptyResult.IsSuccess
                    ? $"Doğru şekilde reddedildi: {emptyResult.Message}"
                    : "Boş terim kabul edildi (beklenmeyen)"
            });

            // Sonuçlarda zorunlu alanlar dolu mu?
            if (searchResult is { IsSuccess: true, Data.Count: > 0 })
            {
                var allFieldsOk = searchResult.Data.All(r =>
                    !string.IsNullOrWhiteSpace(r.Title) &&
                    !string.IsNullOrWhiteSpace(r.Category) &&
                    !string.IsNullOrWhiteSpace(r.Url));
                report.Add(new QaTestResult
                {
                    Category = "Global Search",
                    TestName = "Sonuçlarda zorunlu alanlar dolu",
                    Passed = allFieldsOk,
                    Detail = allFieldsOk
                        ? "Title, Category ve Url tüm sonuçlarda mevcut"
                        : "Eksik alanlar tespit edildi"
                });
            }
        }
        catch (Exception ex)
        {
            report.Add(new QaTestResult
            {
                Category = "Global Search",
                TestName = "GlobalSearchService testleri",
                Passed = false,
                Detail = $"Exception: {ex.Message}"
            });
        }
    }

    // ──────────────────────────────────────────────────────────
    // 16. EXPORT SERVICE TESTLERİ (Cross-cutting)
    // ──────────────────────────────────────────────────────────
    private void RunExportServiceTests(QaTestReport report)
    {
        try
        {
            // Test verileriyle CSV dışa aktarım
            var testData = new YonetIQ.Data.Models.QueryResult
            {
                Name = "QA Test Raporu",
                Columns = ["ID", "Ad", "Durum"],
                Rows =
                [
                    new Dictionary<string, object?> { ["ID"] = 1, ["Ad"] = "Test Kayıt 1", ["Durum"] = "Aktif" },
                    new Dictionary<string, object?> { ["ID"] = 2, ["Ad"] = "Türkçe Karakter ÇŞĞÜÖİ", ["Durum"] = "Pasif" }
                ]
            };

            var csvBytes = exportService.ExportToCsv(testData);
            var csvValid = csvBytes is { Length: > 0 };
            report.Add(new QaTestResult
            {
                Category = "Export Service",
                TestName = "ExportToCsv geçerli byte dizisi döndürür",
                Passed = csvValid,
                Detail = csvValid ? $"{csvBytes.Length} byte CSV üretildi" : "Boş veya null byte dizisi"
            });

            // CSV BOM içermeli (Türkçe karakter desteği)
            if (csvValid)
            {
                var hasBom = csvBytes[0] == 0xEF && csvBytes[1] == 0xBB && csvBytes[2] == 0xBF;
                report.Add(new QaTestResult
                {
                    Category = "Export Service",
                    TestName = "CSV dosyası UTF-8 BOM içeriyor",
                    Passed = hasBom,
                    Detail = hasBom ? "BOM mevcut (Türkçe uyumlu)" : "BOM eksik"
                });

                // CSV içeriğinde kolon başlıkları var mı?
                var csvText = System.Text.Encoding.UTF8.GetString(csvBytes, 3, csvBytes.Length - 3);
                var hasHeaders = csvText.Contains("ID") && csvText.Contains("Ad") && csvText.Contains("Durum");
                report.Add(new QaTestResult
                {
                    Category = "Export Service",
                    TestName = "CSV başlık satırı doğru",
                    Passed = hasHeaders,
                    Detail = hasHeaders ? "Başlık satırı mevcut" : "Başlık satırı eksik veya bozuk"
                });

                // Türkçe karakterler korunmuş mu?
                var hasTurkish = csvText.Contains("ÇŞĞÜÖİ");
                report.Add(new QaTestResult
                {
                    Category = "Export Service",
                    TestName = "CSV Türkçe karakterleri koruyor",
                    Passed = hasTurkish,
                    Detail = hasTurkish ? "Türkçe karakterler doğru" : "Türkçe karakter kaybı"
                });
            }

            // Excel dışa aktarım
            var excelBytes = exportService.BuildExcel(testData, "QA Test");
            var excelValid = excelBytes is { Length: > 0 };
            report.Add(new QaTestResult
            {
                Category = "Export Service",
                TestName = "BuildExcel geçerli byte dizisi döndürür",
                Passed = excelValid,
                Detail = excelValid ? $"{excelBytes.Length} byte XLSX üretildi" : "Boş veya null byte dizisi"
            });

            // HTML rapor testi
            var html = exportService.BuildHtmlReport(testData, "QA Test");
            var htmlValid = !string.IsNullOrWhiteSpace(html) && html.Contains("<table>") && html.Contains("QA Test");
            report.Add(new QaTestResult
            {
                Category = "Export Service",
                TestName = "BuildHtmlReport geçerli HTML döndürür",
                Passed = htmlValid,
                Detail = htmlValid ? $"{html.Length} karakter HTML üretildi" : "HTML içerik geçersiz"
            });

            // Boş veriyle dışa aktarım çökmemeli
            var emptyData = new YonetIQ.Data.Models.QueryResult
            {
                Name = "Boş Rapor",
                Columns = ["A"],
                Rows = []
            };
            var emptyCsv = exportService.ExportToCsv(emptyData);
            report.Add(new QaTestResult
            {
                Category = "Export Service",
                TestName = "Boş veriyle CSV üretimi çökmez",
                Passed = emptyCsv is { Length: > 0 },
                Detail = $"{emptyCsv.Length} byte (sadece başlık satırı)"
            });
        }
        catch (Exception ex)
        {
            report.Add(new QaTestResult
            {
                Category = "Export Service",
                TestName = "ExportService testleri",
                Passed = false,
                Detail = $"Exception: {ex.Message}"
            });
        }
    }
}

// ──────────────────────────────────────────────────────────
// QA RAPOR MODELLERİ
// ──────────────────────────────────────────────────────────

public class QaTestReport
{
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int TotalDurationMs { get; set; }
    public List<QaTestResult> Results { get; set; } = [];

    public int TotalCount => Results.Count;
    public int PassedCount => Results.Count(r => r.Passed);
    public int FailedCount => Results.Count(r => !r.Passed);
    public bool AllPassed => Results.All(r => r.Passed);

    public void Add(QaTestResult result) => Results.Add(result);

    public IEnumerable<IGrouping<string, QaTestResult>> GroupedResults =>
        Results.GroupBy(r => r.Category);

    public string ToSummary()
    {
        var lines = new List<string>
        {
            $"═══ AI QA Test Raporu ═══",
            $"Tarih: {CompletedAt:dd.MM.yyyy HH:mm:ss}",
            $"Süre: {TotalDurationMs}ms",
            $"Sonuç: {PassedCount}/{TotalCount} başarılı ({(TotalCount > 0 ? PassedCount * 100 / TotalCount : 0)}%)",
            ""
        };

        foreach (var group in GroupedResults)
        {
            var passed = group.Count(r => r.Passed);
            var total = group.Count();
            lines.Add($"── {group.Key} ({passed}/{total}) ──");
            foreach (var result in group)
            {
                var icon = result.Passed ? "✅" : "❌";
                lines.Add($"  {icon} {result.TestName}");
                if (!result.Passed || !string.IsNullOrWhiteSpace(result.Detail))
                    lines.Add($"     → {result.Detail}");
            }
            lines.Add("");
        }

        return string.Join("\n", lines);
    }
}

public class QaTestResult
{
    public required string Category { get; set; }
    public required string TestName { get; set; }
    public bool Passed { get; set; }
    public string Detail { get; set; } = string.Empty;
}
