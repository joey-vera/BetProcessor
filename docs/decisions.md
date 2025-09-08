# Decision Log

•	Minimal API project is selected for the Rest API solution which are better suited for small, fast, and simple applications.
•	Producer-Consumer pattern is used to handle the incoming bets (BetQueueService) and process them (WorkerService) in the background.
•	System.Threading.Channels (async queue) is used to store the bets in a queue.
•	Worker pattern is used as Background service (WorkerService), injected as HostedService to execute background tasks at application initialization and controled termination on application ending.
•	Observer pattern is used to notify workers when shutting down the application and stop gracefully.
•	Domain-Driven Design is used to structure the code into layers (Application, Domain) keeping business logic independent of transport layer (REST API).
•	Configure Swagger/JSON serialization to show enum names (not just integers).
•	Configure Swagger to use the XML docs.