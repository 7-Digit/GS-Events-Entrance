// @ts-nocheck
import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";

import "./index.css";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

const queryClient = new QueryClient();

const Component = () => {
  const ipcRenderer = (window as any).ipcRenderer;
  const [readerConnected, setReaderConnected] = useState(false);

  const [scannedCardData, setScannedCardData] = useState({
    valid: null,
    isScanned: false,
    ownerName: "",
    cardSeqNumber: "",
    entries: [],
    totalEntries: 0,
    isExpired: false,
    hasEntryInLastPeriod: false,
  });

  useEffect(() => {
    ipcRenderer.send("connect-place");
  }, []);

  const [placeData, setPlaceData] = useState({
    displayData: false,
    isConnected: false,
    macAddress: "",
    hasTriedToConnect: false,
    name: "",
  });

  ipcRenderer.on("place-connected", (response) => {
    if (!response.success) {
      setPlaceData({
        name: "",
        displayData: true,
        isConnected: false,
        macAddress: response.macAddress,
        hasTriedToConnect: true,
      });
    } else {
      setPlaceData({
        name: response.name,
        displayData: true,
        isConnected: true,
        macAddress: response.physical_id,
        hasTriedToConnect: true,
      });
    }
    console.log("Place connected", response);
  });

  ipcRenderer.on("card-scanned", (response) => {
    const isValid = response.valid;

    const firstUse = new Date(response.cardData?.firstUse).toLocaleString(
      "bg-BG"
    );
    const validTo = new Date(response.cardData?.validTo).toLocaleString(
      "bg-BG"
    );

    setScannedCardData({
      isScanned: true,
      valid: isValid,
      ownerName: response.ownerName,
      cardSeqNumber: response.cardData?.seqNumber,
      entries: response.serviceUsages,
      totalEntries: response.totalUsages,
      isExpired: response.isExpired,
      firstUse,
      validTo,
      hasEntryInLastPeriod: response.hasEntryInLastPeriod,
    });
  });

  ipcRenderer.on("reader-connected", () => {
    console.log("Reader connected");
    setReaderConnected(true);
  });

  ipcRenderer.on("reader-disconnected", () => {
    console.log("Reader disconnected");
    setReaderConnected(false);
  });
  console.log(scannedCardData, "data");
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        justifyContent: "flex-start",
        alignItems: "center",
        width: "100%",
        flex: 1,
        height: "100%",
      }}
    >
      {readerConnected && (
        <div>
          <h4>Четецът е свързан</h4>
        </div>
      )}

      {placeData.displayData ? (
        placeData.isConnected ? (
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              justifyContent: "center",
              alignItems: "center",
            }}
          >
            <h2>Обектът е свързан</h2>
            <p>{placeData.name}</p>
          </div>
        ) : (
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              justifyContent: "center",
              alignItems: "center",
            }}
          >
            <h2>Обектът не е регистриран</h2>
            <p>{placeData.macAddress}</p>
          </div>
        )
      ) : null}

      {scannedCardData.isScanned && (
        <div
          style={{
            backgroundColor: scannedCardData.valid
              ? scannedCardData.hasEntryInLastPeriod
                ? "orange"
                : "green"
              : "red",
            width: "100vw",
            height: "100%",
            paddingVertical: "2rem",
            color: "white",
          }}
        >
          {scannedCardData ? (
            <div
              style={{
                color: "white",
                justifyContent: "center",
                alignItems: "center",
                display: "flex",
                flexDirection: "column",
              }}
            >
              {!scannedCardData.valid && (
                <h1 style={{ textAlign: "center" }}>
                  {scannedCardData.isExpired
                    ? "Картата е изтекла"
                    : "Невалидна карта"}
                </h1>
              )}
              {scannedCardData.ownerName && scannedCardData.cardSeqNumber && (
                <h3>
                  {`Притежател - ${scannedCardData.ownerName}`}
                  <br />
                  {`Номер на карта - ${scannedCardData.cardSeqNumber}`}
                  <br />
                  <br />
                  {`Дата на валидност - ${scannedCardData.validTo}`}
                  <br />
                  {`Дата на активация - ${scannedCardData.firstUse}`}
                </h3>
              )}
              <h4>
                {scannedCardData.totalEntries &&
                  "Общо влизания - " + scannedCardData.totalEntries}
                <br />
                {scannedCardData.entries?.length && "Последни 10 влизания"}
              </h4>
              {scannedCardData.entries?.map((x, i) => {
                const date = new Date(x.used_at);
                const day = date.toLocaleDateString("bg-BG", {
                  day: "2-digit",
                  month: "2-digit",
                });
                const time = date.toLocaleTimeString();
                return <h4 key={i}>{`${i + 1}. ${day} - ${time}`}</h4>;
              })}
            </div>
          ) : null}
        </div>
      )}
    </div>
  );
};

createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <Component />
    </QueryClientProvider>
  </React.StrictMode>
);
