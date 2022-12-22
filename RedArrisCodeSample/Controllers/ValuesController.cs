using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using IEXSharp.Model.CoreData.StockPrices.Response;

namespace RedArrisCodeSample.Controllers
{
   
    [ApiController]
    public class ValuesController : ControllerBase
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly string BASE_URI = "https://cloud.iexapis.com/v1/data/CORE/";
        private static readonly string IEX_AUTH_TOKEN = "";//REQUIRED FOR ALL APIS
        private const string DATE_MIN = "0001-01-01";
        private const string DATE_MAX = "3001-01-01";

        public ValuesController(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

        [Route("api/getreturn")]
        public async Task<IActionResult> GetReturnAsync(string StockSymbol, string? FromDate = DATE_MIN, string? ToDate = DATE_MAX)
        {

            var iexAuthProblem = ValidateIexAuthToken();
            if (iexAuthProblem != null) { return iexAuthProblem; }

            var dateValidationProblem = ValidateAndFormatDates(ref ToDate, ref FromDate);
            if (dateValidationProblem != null) { return dateValidationProblem; }

            var stockSymbolValidationProblem = await ValidateStockSymbolAsync(StockSymbol);
            if (stockSymbolValidationProblem != null) { return stockSymbolValidationProblem; }

            var historicalPriceResponses = await RetrieveHistoricalDataAsync(StockSymbol, FromDate, ToDate);


            return Ok(historicalPriceResponses.Select(hpr => (hpr.close - hpr.open).ToString()));
        }


        [Route("api/getalpha")]
        public async Task<IActionResult> GetAlphaAsync(string StockSymbol, string BenchmarkStockSymbol, string? FromDate = DATE_MIN, string? ToDate = DATE_MAX)
        {
            var iexAuthProblem = ValidateIexAuthToken();
            if(iexAuthProblem != null) { return iexAuthProblem; }

            var dateValidationProblem = ValidateAndFormatDates(ref ToDate, ref FromDate);
            if (dateValidationProblem != null) { return dateValidationProblem; }

            var stockSymbolValidationProblem = await ValidateStockSymbolAsync(StockSymbol);
            if (stockSymbolValidationProblem != null) { return stockSymbolValidationProblem; }

            var benchmarkStockSymbolValidationProblem = await ValidateStockSymbolAsync(BenchmarkStockSymbol);
            if (benchmarkStockSymbolValidationProblem != null) { return benchmarkStockSymbolValidationProblem; }

            var historicalPriceResponsesTask = RetrieveHistoricalDataAsync(StockSymbol, FromDate, ToDate);
            var benchmarkHistoricalPriceResponsesTask = RetrieveHistoricalDataAsync(BenchmarkStockSymbol, FromDate, ToDate) ;

            var historicalPriceResponses = await historicalPriceResponsesTask;
            var benchmarkHistoricalPriceResponses = await benchmarkHistoricalPriceResponsesTask;

            //Calculate alpha
            var stockReturn = (historicalPriceResponses.LastOrDefault()?.close - historicalPriceResponses.FirstOrDefault()?.open) / historicalPriceResponses.FirstOrDefault()?.open;
            var benchmarkStockReturn = (benchmarkHistoricalPriceResponses.LastOrDefault()?.close - benchmarkHistoricalPriceResponses.FirstOrDefault()?.open) / benchmarkHistoricalPriceResponses.FirstOrDefault()?.open;

            //simplified alpha calculation, would use a finance resource to deduce how exactly they want this done
            //or pull the formula from their excel
            var alpha = (stockReturn - benchmarkStockReturn) * 100;

            return alpha != null ? Ok(Math.Round((decimal)alpha, 2).ToString() + "%") : StatusCode(500);
        }

        private ActionResult? ValidateAndFormatDates(ref string ToDate, ref string FromDate)
        {

            if (!DateTime.TryParse(ToDate, out var toDate))
            {
                return ValidationProblem("DateTime.TryParse failed on ToDate Value, recommended format 'MM/dd/yyyy'");
            }
            if (!DateTime.TryParse(FromDate, out var fromDate))
            {
                return ValidationProblem("DateTime.TryParse failed on ToDate Value, recommended format 'MM/dd/yyyy'");
            }
            if (fromDate > toDate)
            {
                return ValidationProblem("FromDate is greater than ToDate, range invalid");
            }
            ToDate = toDate.ToString("yyyy-MM-dd");
            FromDate = fromDate.ToString("yyyy-MM-dd");
            return null;
        }

        //TODO improve resouce use to test validation, Couldnt find anything in IEX core
        //Potentially our own list of valid Stock Tickers
        //https://cloud.iexapis.com/beta/ref-data/symbols in beta
        private async Task<IActionResult?> ValidateStockSymbolAsync(string StockSymbol)
        {
            var uri = $"{BASE_URI}COMPANY/{StockSymbol}?token={IEX_AUTH_TOKEN}";
            var httpClient = _httpClientFactory.CreateClient();//unsure if this is the best pattern, creating it every call, would like to discuss
            var response = await httpClient.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                return string.IsNullOrEmpty(responseBody.Replace("[]", "")) ?
                    ValidationProblem("StockSymbol Invalid, please pass valid symbol")
                   : null;

            }

            return BadRequest();

          
        }

        private IActionResult? ValidateIexAuthToken()
        {
            return string.IsNullOrEmpty(IEX_AUTH_TOKEN) ?
                    StatusCode(500, "App is missing IEX auth token, APIs will not work")
                   : null;
        }


        private async Task<List<HistoricalPriceResponse>> RetrieveHistoricalDataAsync(string StockSymbol, string FromDate, string ToDate)
        {
            var uri = $"{BASE_URI}HISTORICAL_PRICES/{StockSymbol}?token={IEX_AUTH_TOKEN}&from={FromDate}&to={ToDate}";
            var httpClient = _httpClientFactory.CreateClient();//unsure if this is the best pattern, creating it every call, would like to discuss
            var response = await httpClient.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                List<HistoricalPriceResponse> historicalPriceResponses = JsonConvert.DeserializeObject<List<HistoricalPriceResponse>>(responseBody).OrderBy(hpr => hpr.date).ToList();
                return historicalPriceResponses ?? new List<HistoricalPriceResponse>();
            }
           
            return new List<HistoricalPriceResponse>();
        }

    }
}
