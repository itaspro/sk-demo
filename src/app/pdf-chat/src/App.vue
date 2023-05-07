<template>
  <div id="app">
      <vue-advanced-chat
        height="calc(100vh - 20px)"
        :current-user-id="clientID"
        :rooms="JSON.stringify(rooms)"
        :messages="JSON.stringify(messages)"
        :messages-loaded="messagesLoaded"
        @send-message="sendMessage($event.detail[0])"
        @fetch-messages="fetchMessages($event.detail[0])"
        show-add-room="true"
        show-files="true"
        :rooms-loaded = "true"
      />
  </div>
</template>

<script>
import axios from "axios";
import { HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr'
import { register } from 'vue-advanced-chat'
register()

const initSignalR = (state) => {
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
    state.error = JSON.parse(error)
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

export default {
  name: "App",
  data() {
    return {
      selectedFile: null,
      progress: null,
      clientID : null,
      error: null,
      connected: false,
      reconnecting: false,
			rooms: [],
			messages: [],
			messagesLoaded: true,
    };
  },
  created () {
    this.signalr = initSignalR(this)
  },
  methods: {
    async uploadPdf(files, message) {
      if (files.length ==0) {
        alert("Please select a PDF file.");
        return false;
      }
      this.messagesLoaded = false;
      const formData = new FormData();
 
      for (let i in files) {
        let file = files[i];
        if (file.extension != "pdf") {
          alert("Please select a PDF file.");
          return false;
        }
        formData.append("file", new File([file.blob], `${file.name}.${file.extension}` ));
      }
      formData.append("message", message);

      try {
        const response = await axios.post("https://localhost:7266/api/fileupload/"+this.clientID, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
        });
        console.log(response.data);
      } catch (error) {
        console.error("Error uploading file:", error);
        return false;
      }
      return true;
    },
    async start() {
        try {
            await this.signalr.start();
            console.log("SignalR Connected.");
            this.connected = true;
        } catch (err) {
            console.log(err);
            setTimeout(this.start, 5000);
        }
        this.reconnecting = false;
    },
    fetchMessages() {
      this.messagesLoaded = true
		},
		async sendMessage(message) {
      if (message.files && message.files.length > 0) {
        await this.uploadPdf(message.files, message.content)
        
      } else {
        let mId = this.messages.length;
        await this.signalr.invoke("Ask",message.content, mId); 
        this.messages = [
          ...this.messages,
          {
            _id: mId,
            content: message.content,
            senderId: 1,
            timestamp: new Date().toString().substring(16, 21),
            date: new Date().toDateString()
          }
        ]
      }
		},
  },
  mounted() {
    this.start();
    this.rooms = [...this.rooms,   
    {
      roomId: '1',
      roomName: 'PDF Chat',
      avatar: 'https://www.techopedia.com/wp-content/uploads/2023/03/6e13a6b3-28b6-454a-bef3-92d3d5529007.jpeg',
      unreadCount: 1,
      index: 1,
      users:[
        {
            _id: '1',
            username: 'Demo User',
            avatar: 'https://media.licdn.com/dms/image/C5603AQFZ5CsDDBN8Qg/profile-displayphoto-shrink_100_100/0/1516584677087?e=1689206400&v=beta&t=VKwPOI-mdi7C6ESVuGdt6uBdSNlX5S3moO204j4x6G8',
            status: {
              state: 'online',
            }
        },
      ]}
    ];

    this.messages = [
      {
        _id: this.messages.length+1,
        content: "Please upload PDFs files to start the conversation.",
        senderId: 0,
        timestamp: new Date().toString().substring(16, 21),
        date: new Date().toDateString()
      },
      ...this.messages
    ]
  }
};

</script>