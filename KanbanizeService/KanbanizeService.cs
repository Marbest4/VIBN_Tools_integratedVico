using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VIBN_Tools.KanbanizeService
{
    public class KanbanizeService
    {
        public static string ApiKey =>
            (Environment.GetEnvironmentVariable(
                 "VIBN_VICO_KANBANIZE_API_KEY",
                 EnvironmentVariableTarget.User) ??
             Environment.GetEnvironmentVariable("VIBN_VICO_KANBANIZE_API_KEY"))?.Trim() ?? string.Empty;

        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _throttle = new SemaphoreSlim(10);    // max. 10 parallel



        public KanbanizeService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            if (!string.IsNullOrWhiteSpace(ApiKey))
                _httpClient.DefaultRequestHeaders.Add("apikey", ApiKey);
            _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
        }



        public async Task<List<KanbanizeBoard>> LoadBoardsAsync()
        {
            string url = "https://grobgroup.kanbanize.com/api/v2/boards";

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();

            var dtoResponse = await JsonSerializer.DeserializeAsync<KanbanizeBoardListResponseDto>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

            return dtoResponse.data.Select(KanbanizeMapping.MapBoard).Where(b => b.IsArchived == false).ToList();
        }




        public async Task<List<KanbanizeWorkflow>> LoadWorkflowsAsync(int boardId)
        {
            string url = $"https://grobgroup.kanbanize.com/api/v2/boards/{boardId}/workflows";

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();

            var dtoResponse = await JsonSerializer.DeserializeAsync<KanbanizeWorkflowListResponseDto>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

            return dtoResponse.data.Select(KanbanizeMapping.MapWorkflow).ToList();
        }


        public async Task<List<KanbanizeLane>> LoadLanesAsync(int boardId)
        {
            string url = $"https://grobgroup.kanbanize.com/api/v2/boards/{boardId}/lanes";

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();

            var dtoResponse = await JsonSerializer.DeserializeAsync<KanbanizeLaneListResponseDto>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return dtoResponse.data.Select(KanbanizeMapping.MapLane).ToList();
        }



        public async Task<List<KanbanizeColumn>> LoadColumnsAsync(int boardId)
        {
            string url = $"https://grobgroup.kanbanize.com/api/v2/boards/{boardId}/columns";

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();

            var dtoResponse = await JsonSerializer.DeserializeAsync<KanbanizeColumnListResponseDto>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return dtoResponse.data.Select(KanbanizeMapping.MapColumn).ToList();
        }



        public async Task<List<KanbanizeCard>> LoadAllCardsAsync()
        {
            var allCards = new ConcurrentBag<KanbanizeCard>();

            // Load first page (has pagination information)
            var firstPage = await LoadCardsPageAsync(1);

            foreach (var card in firstPage.data.data)
                allCards.Add(KanbanizeMapping.MapCard(card));

            int totalPages = firstPage.data.pagination.all_pages;

            // Load all other pages parallel
            var tasks = Enumerable.Range(2, totalPages - 1).Select(async page =>
            {
                var result = await LoadCardsPageThrottledAsync(page);
                foreach (var dto in result.data.data)
                    allCards.Add(KanbanizeMapping.MapCard(dto));
            });

            await Task.WhenAll(tasks);


            return allCards.ToList();

        }



        private async Task<KanbanizeCardListResponseDto> LoadCardsPageAsync(int page)
        {
            var url = $"https://grobgroup.kanbanize.com/api/v2/cards?page={page}&results_per_page=500";

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();

            return await JsonSerializer.DeserializeAsync<KanbanizeCardListResponseDto>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }


        private async Task<KanbanizeCardListResponseDto> LoadCardsPageThrottledAsync(int page)
        {
            await _throttle.WaitAsync();
            try
            {
                return await LoadCardsPageAsync(page);
            }
            finally
            {
                _throttle.Release();
            }
        }







    }
}
