<template>
  <div id="app">
    <h1>PDF Uploader</h1>
    <input type="file" @change="onFileSelected" accept=".pdf" />
    <button @click="uploadPdf">Upload PDF</button>
    <div v-if="progress > 0">
      <h2>Progress: {{ progress }}%</h2>
      <progress :value="progress" max="100"></progress>
    </div>
  </div>
</template>

<script>
import axios from "axios";
import { useSignalR } from '@dreamonkey/vue-signalr';

export default {
  name: "App",
  data() {
    return {
      selectedFile: null,
      progress: 0,
      clientID : null,
    };
  },
  setup() {
    const signalr = useSignalR();
    signalr.on('Connected', ({ clientID }) => {
      this.clientID = clientID;
    });
  },
  methods: {
    onFileSelected(event) {
      this.selectedFile = event.target.files[0];
    },
    async uploadPdf() {
      if (!this.selectedFile) {
        alert("Please select a PDF file.");
        return;
      }

      const formData = new FormData();
      formData.append("file", this.selectedFile);

      try {
        const response = await axios.post("https://localhost:7266/api/fileupload/"+this.clientID, formData);
        console.log(response.data);
      } catch (error) {
        console.error("Error uploading file:", error);
      }
    },
    progressUpdate(data) {
      this.progress = data.Progress;
    },
  },
  mounted() {
    // this.$signalr.start({
    //   url: "https://localhost:7266/Hub",
    // });
    // this.$signalr.on("Connected", (e) => console.log(e));
    // this.$signalr.on("ProgressUpdate", this.progressUpdate);
  }
};
</script>