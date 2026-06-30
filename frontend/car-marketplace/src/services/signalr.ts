import * as signalR from "@microsoft/signalr"

let connection: signalR.HubConnection | null = null

export const startConnection = async (token: string) => {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    return connection
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5127/hubs/chat", {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .build()

  await connection.start()
  return connection
}

export const getConnection = () => connection