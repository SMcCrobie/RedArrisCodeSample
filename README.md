# RedArrisCodeSample
Simple WebApp with 2 APIs

/RedArrisCodeSample/Controllers/ValuesController.cs requires a auth token to IEX to be filled in to correctly run


API formats

GetReturn example
https://localhost:7162/api/getreturn?ToDate=2022-02-15&FromDate=2022-02-13&StockSymbol=tsla

GetAlpha example
https://localhost:7162/api/getalpha?ToDate=2022-02-15&FromDate=2022-02-13&StockSymbol=tsla&BenchmarkStockSymbol=SPXC
