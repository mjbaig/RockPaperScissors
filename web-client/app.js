var connection = new signalR.HubConnectionBuilder().withUrl("https://localhost:7116/game", {
    skipNegotiation: true,
    transport: signalR.HttpTransportType.WebSockets
})
    .build();


connection.start().then(e => console.log(`started ${e}`)).catch(e => console.error(e));

function signIn(name) {
    connection.invoke("signIn", name).then(e => console.log(`hi ${e}`)).catch(e => console.log(e));
}

function joinQueue(name) {
    connection.invoke("joinQueue", name).then(e => console.log(`hi ${e}`)).catch(e => console.log(e));
}

connection.on("getState", (e) => console.log(e));

console.log("loaded");