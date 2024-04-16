// See the Electron documentation for details on how to use preload scripts:
// https://www.electronjs.org/docs/latest/tutorial/process-model#preload-scripts

import { contextBridge, ipcRenderer } from "electron";

const validChannels = [
  "disconnect-socket",
  "reconnect-socket",
  "connect-place",
  "write-error",
  "place-connected",
  "reader-connected",
  "reader-disconnected",
  "disconnect-success",
  "card-scanned",
];

contextBridge.exposeInMainWorld("ipcRenderer", {
  send: (channel: string, data: any) => {
    // whitelist channels
    console.log("send to channel - ", channel);
    if (validChannels.includes(channel)) {
      console.log("Success send");
      ipcRenderer.send(channel, data);
    }
  },
  on: (channel: string, func: any) => {
    if (validChannels.includes(channel)) {
      // Deliberately strip event as it includes `sender`
      ipcRenderer.on(channel, (event, ...args) => func(...args));
    }
  },

  receive: (channel: string, func: any) => {
    if (validChannels.includes(channel)) {
      // Deliberately strip event as it includes `sender`
      ipcRenderer.on(channel, (event, ...args) => func(...args));
    }
  },
});
