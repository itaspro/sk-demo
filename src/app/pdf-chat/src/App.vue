<template>
  <div id="app">
    
    <!-- <h1>PDF Uploader</h1>
    <input type="file" @change="onFileSelected" accept=".pdf" />
    <button @click="uploadPdf">Upload PDF</button>
    <div v-if="progress">
      <h2>Progress: {{ progress.Message }}%</h2>
      <progress :value="progress.Progress" max="100"></progress>
    </div>
    <div>
      connected: {{ connected }}
      reconnecting: {{ reconnecting }}
      <button :disabled="connected" @click="start">Reconnect</button>
    </div> -->
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
    state.sendMessage("Please upload a PDF file");
  });

  signalr.on('Progress', progress => {
    state.progress= JSON.parse( progress)
  }); 

  signalr.on('Complete', () => {
    state.progress= null
  });

  signalr.on('Error', error => {
    state.error = JSON.parse(error)
  });

  signalr.on("Reply", answer => {
    // console.log(state.clientID, state.messages);
    state.messages.push(
      {
        _id: state.messages.length,
        content: answer,
        senderId: state.clientID,
        timestamp: new Date().toString().substring(16, 21),
        date: new Date().toDateString()
      }
    )
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
			messagesLoaded: false,
    };
  },
  created () {
    this.signalr = initSignalR(this)
  },
  methods: {
    async uploadPdf(files) {
      if (files.length ==0) {
        alert("Please select a PDF file.");
        return;
      }

      const formData = new FormData();
 
      for (let i in files) {
        formData.append("file", new File([files[i].blob], files[i].name) );
      }
      //formData.append("file", files[0].blob);
      try {
        const response = await axios.post("https://localhost:7266/api/fileupload/"+this.clientID, formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          }
      });
        console.log(response.data);
      } catch (error) {
        console.error("Error uploading file:", error);
      }
    },
    progressUpdate(data) {
      this.progress = data.Progress
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
        await this.uploadPdf(message.files, message.content);
      } else {
        await this.signalr.invoke("Echo",  "message");
        this.messages = [
          {
            _id: this.messages.length,
            content: message.content,
            senderId: this.currentUserId,
            timestamp: new Date().toString().substring(16, 21),
            date: new Date().toDateString()
          },
          ...this.messages
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
      avatar: 'assets/imgs/people.png',
      unreadCount: 1,
      index: 1,
      users:[
        {
            _id: '1234',
            username: 'John Doe',
            avatar: 'assets/imgs/doe.png',
            status: {
              state: 'online',
              lastChanged: 'today, 14:30'
            }
        },
      ]}
    ]
  }
};

</script>