// @ts-nocheck

import { app, BrowserWindow, ipcMain, webContents } from "electron";
// This allows TypeScript to pick up the magic constants that's auto-generated by Forge's Webpack
// plugin that tells the Electron app where to look for the Webpack-bundled app code (depending on
// whether you're running in development or production).
declare const MAIN_WINDOW_WEBPACK_ENTRY: string;
declare const MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY: string;

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require("electron-squirrel-startup")) {
  app.quit();
}

const createWindow = (): void => {
  // Create the browser window.
  const mainWindow = new BrowserWindow({
    height: 600,
    width: 800,
    webPreferences: {
      preload: MAIN_WINDOW_PRELOAD_WEBPACK_ENTRY,
      nodeIntegration: true,
      // nodeIntegrationInWorker: true,
      // contextIsolation: false,
    },
  });

  // and load the index.html of the app.
  mainWindow.loadURL(MAIN_WINDOW_WEBPACK_ENTRY);

  // Open the DevTools.
  mainWindow.webContents.openDevTools();
};

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.on("ready", () => {
  createWindow();
});

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});

const disconnectSocket = () => {
  socket?.disconnect();
  socket?.off();
};

// app.on("will-quit", () => {
//   console.log("App is quitting");
//   disconnectSocket();
// });

// app.on("before-quit", () => {
//   disconnectSocket();
// });

// app.on("render-process-gone", () => {
//   disconnectSocket();
// });

app.on("activate", () => {
  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  if (BrowserWindow.getAllWindows().length === 0) {
    createWindow();
  }
});

let nfcReader = null;
let socket;

app.on("web-contents-created", (_, webContents) => {
  if (nfcReader) {
    setTimeout(() => {
      webContents.send("reader-connected");
    }, 2000);
  }
});

// import { io } from "socket.io-client";
import { NFC } from "nfc-pcsc";
import getMAC from "getmac";

const nfc = new NFC(); // optionally you can pass logger
const API_URL = "http://localhost:3001/api/v1";
const ADMIN_URL = API_URL + "/admin";
const CARD_URL = API_URL + "/cards";

const handleConnectReader = () => {
  const browserWindow = BrowserWindow.getAllWindows()[0];
  browserWindow?.webContents?.send("reader-connected");
};

const handleDisconnectReader = () => {
  const browserWindow = BrowserWindow.getAllWindows()[0];
  browserWindow?.webContents?.send("reader-disconnected");
};

const handleSendEventToRenderer = (channel, data) => {
  const browserWindow = BrowserWindow.getAllWindows()[0];
  browserWindow?.webContents?.send(channel, data);
};

let placeId, physicalDeviceId;

nfc.on("reader", (reader) => {
  nfcReader = reader;

  handleConnectReader();

  reader.on("card", async (card) => {
    const response = await fetch(CARD_URL + "/scan-card", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ cardId: card.uid, placeId, physicalDeviceId }),
    });

    const result = await response.json();

    if (result.valid) {
      handleSendEventToRenderer("card-scanned", result);
    } else {
      handleSendEventToRenderer("card-scanned", result);
    }

    // socket.emit("scan card", { cardId: card.uid });
  });

  reader.on("error", (err) => {
    console.log(`${reader.reader.name}  an error occurred`, err);
  });

  reader.on("end", () => {
    console.log(`${reader.reader.name}  device removed`);
    handleDisconnectReader();
  });
});

ipcMain.on("reconnect-socket", async (event, data) => {
  event.reply("connection-success", "Socket connection successful");
});

ipcMain.on("disconnect-socket", async (event, data) => {
  disconnectSocket();
  event.reply("disconnect-success", "Data written to card successfully");
});

ipcMain.on("connect-place", async (event, data) => {
  const macAddress = getMAC();

  const response = await fetch(
    ADMIN_URL + `/place-by-mac-address?macAddress=${macAddress}`
  );
  const result = await response.json();

  placeId = result.place_id;
  physicalDeviceId = result.physical_device_id;

  event.sender.send("place-connected", result);
});
