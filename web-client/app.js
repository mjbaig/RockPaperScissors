var connection = new signalR.HubConnectionBuilder().withUrl("https://localhost:7116/game", {
    skipNegotiation: true,
    transport: signalR.HttpTransportType.WebSockets
})
    .build();


connection.start().then(e => console.log(`started ${e}`)).catch(e => console.error(e));

connection.invoke("SignIn", "neebu").then(e => console.log(`hi ${e}`)).catch(e => console.log(e));