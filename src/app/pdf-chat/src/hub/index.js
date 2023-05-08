import { HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr'
const initHub = (state) => {
  const signalr = new HubConnectionBuilder()
    .withUrl("https://localhost:7266/hub")
    .configureLogging(LogLevel.Information)
    .withAutomaticReconnect()
    .build()

  signalr.onclose(async (error) => {
    console.assert(signalr.state === HubConnectionState.Disconnected);
    state.reconnecting = true;
    state.connected = false;
    console.log("disconnected:" + error)
    await state.start();
  })

  signalr.on('Connected', ({ clientID }) => {
    state.clientID = clientID;
  });

  signalr.on('Progress',  (Message)  => {
    var progrss = {
              _id: state.messages.length,
              content: Message,
              senderId: 0,
              timestamp: new Date().toString().substring(16, 21),
              date: new Date().toDateString()
            }
    state.messages[state.messages.length-1] = progrss;
  }); 

  signalr.on('Complete', (Message) => {
    var progrss = {
              _id: state.messages.length,
              content: Message,
              senderId: 0,
              timestamp: new Date().toString().substring(16, 21),
              date: new Date().toDateString()
            }
    state.messages[state.messages.length-1] = progrss;
    state.messagesLoaded = true;
  });

  signalr.on('Error', error => {
    state.errors = [JSON.parse(error)]
  });

  signalr.on("Reply", (answer, seq) => {
    state.messages= [...state.messages,
      {
        _id: seq,
        content: answer,
        senderId: state.clientID,
        timestamp: new Date().toString().substring(16, 21),
        date: new Date().toDateString()
      }
    ]
  })
  return signalr;
}

export {initHub};
