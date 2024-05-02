// @ts-nocheck
import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

import "./index.css";

const queryClient = new QueryClient();

const App = () => {
  const [readerConnected, setReaderConnected] = useState(false);

  const [allEntries, setAllEntries] = useState([]);
  const [enteredValidCards, setEnteredValidCards] = useState();
  const [currentCardEntries, setCurrentCardEntries] = useState([]);

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

  const [placeData, setPlaceData] = useState({
    displayData: false,
    isConnected: false,
    macAddress: "",
    hasTriedToConnect: false,
    name: "",
  });

  useEffect(() => {
    if (scannedCardData.valid) {
      const validEntries = [...allEntries]
        .reverse()
        .filter(
          (x) => x.valid && x.cardSeqNumber === scannedCardData.cardSeqNumber
        );
      setCurrentCardEntries(validEntries);
    } else {
      setCurrentCardEntries([]);
    }
  }, [scannedCardData]);

  useEffect(() => {
    const ipcRenderer = (window as any).ipcRenderer;
    const store = (window as any).electronStore;

    ipcRenderer?.send("connect-place");

    ipcRenderer?.on("place-connected", (response) => {
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
        store.get("entries").then((res) => {
          const entries = res;
          if (!entries) {
            store.set("entries", []);
          } else {
            const resetTime = response.reset_time;
            const resetHour = resetTime.split(":")[0];
            const resetTimeMinutes = resetTime.split(":")[1];
            let currentDayEntries = [];

            const resetDate = new Date();
            resetDate.setHours(Number(resetHour));
            resetDate.setMinutes(Number(resetTimeMinutes));

            const now = new Date();

            if (now.getTime() > resetDate.getTime()) {
              currentDayEntries = entries.filter((x) => {
                return new Date(x.timestamp).getTime() > resetDate.getTime();
              });
            } else {
              const yesterday = new Date(
                new Date().setDate(new Date().getDate() - 1)
              );
              yesterday.setHours(Number(resetHour));
              yesterday.setMinutes(Number(resetTimeMinutes));

              currentDayEntries = entries.filter((x) => {
                return new Date(x.timestamp).getTime() > yesterday.getTime();
              });
            }
            // if (currentDayEntries.length !== entries.length) {
            //   // store.set("entries", currentDayEntries);
            // }

            const validEntries = currentDayEntries.filter((x) => x.valid);
            const uniqueCards = Array.from(
              new Set([...validEntries.map((x) => x.cardSeqNumber)])
            ).length;
            setEnteredValidCards(uniqueCards);

            setAllEntries(currentDayEntries);
          }
        });
      }
    });

    ipcRenderer?.on("reader-connected", () => {
      console.log("Reader connected");
      setReaderConnected(true);
    });

    ipcRenderer?.on("reader-disconnected", () => {
      console.log("Reader disconnected");
      setReaderConnected(false);
    });

    return () => {
      ipcRenderer?.removeAllListeners("place-connected");
      ipcRenderer?.removeAllListeners("card-scanned");
      ipcRenderer?.removeAllListeners("reader-connected");
      ipcRenderer?.removeAllListeners("reader-disconnected");
    };
  }, []);

  useEffect(() => {
    const ipcRenderer = (window as any).ipcRenderer;
    const store = (window as any).electronStore;

    ipcRenderer?.on("card-scanned", (response) => {
      const valid = response.valid;
      const firstUse = response.cardData?.firstUse;
      const validTo = response.cardData?.validTo;

      const firstUseDate = firstUse
        ? new Date(firstUse).toLocaleString("bg-BG")
        : null;
      const validToDate = validTo
        ? new Date(validTo).toLocaleString("bg-BG")
        : null;

      const time = new Date().toLocaleDateString("bg-BG", {
        day: "2-digit",
        month: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
      });

      const data = {
        isScanned: true,
        valid,
        ownerName: response.ownerName,
        cardSeqNumber: response.cardData?.seqNumber,
        entries: response.serviceUsages,
        totalEntries: response.totalUsages + 1,
        isExpired: response.isExpired,
        firstUse: firstUseDate,
        validTo: validToDate,
        hasEntryInLastPeriod: response.hasEntryInLastPeriod,
        isSingleUse: response.isSingleUse,
        confiscateCard: response.confiscateCard,
        serviceName: response.serviceName,
        timestamp: new Date().getTime(),
        time,
        isInactive: response.isInactive,
        companyName: response.companyName,
      };

      setScannedCardData(data);
      setAllEntries((prev) => [...prev, data]);

      const validEntries = [...allEntries, data].filter((x) => x.valid);

      const uniqueCards = Array.from(
        new Set([...validEntries.map((x) => x.cardSeqNumber)])
      ).length;
      setEnteredValidCards(uniqueCards);

      store.get("entries").then((res) => {
        const entries = res;
        const newEntries = [...entries, data];
        store.set("entries", newEntries);
      });
    });

    return () => {
      ipcRenderer?.removeAllListeners("card-scanned");
    };
  }, [allEntries]);

  return (
    <div className="main-container">
      <div>
        {readerConnected ? (
          <h2>Четецът е свързан</h2>
        ) : (
          <h1 style={{ color: "red" }}>НЯМА ВРЪЗКА С ЧЕТЕЦ</h1>
        )}
      </div>

      {placeData.displayData ? (
        placeData.isConnected ? (
          <div className="header">
            <h2>Обектът е свързан - {placeData.name}</h2>
            <Clock />
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

      <h2>Влезли карти - {enteredValidCards || 0}</h2>

      <div className="scanned-cards-container">
        <div className="w-33 flex-col black">
          <h2 className="mtb-1 heading-with-border">Последни влизания</h2>
          {[...allEntries].reverse().map((entry, idx) => {
            return (
              <div
                className={`last-entry bg-${
                  entry.valid
                    ? entry.hasEntryInLastPeriod
                      ? "orange"
                      : "green"
                    : "red"
                }`}
                key={idx + "all-entry"}
              >
                <h4 className="last-entry-text">
                  {entry.cardSeqNumber
                    ? `#${entry.cardSeqNumber + ""} ${
                        entry.ownerName
                          ? `${entry.ownerName} - `
                          : !entry.valid
                          ? "Невалидно - "
                          : ""
                      }`
                    : "Нерегистрирана карта - "}
                  {entry.isExpired
                    ? `  - Услугата "${entry.serviceName}" е изтекла - `
                    : ""}
                  {entry.hasEntryInLastPeriod && entry.valid
                    ? " Повторно влизане - "
                    : ""}
                  {` ${entry.time}`}
                </h4>
              </div>
            );
          })}
        </div>

        <>
          <div
            className={`w-33 flex-col bg-${
              scannedCardData.isScanned
                ? scannedCardData.valid
                  ? scannedCardData.hasEntryInLastPeriod
                    ? "orange"
                    : "green"
                  : "red"
                : ""
            }`}
          >
            <h2 className="middle-section-heading">Текущо</h2>

            {!scannedCardData.valid && (
              <h1
                style={{
                  textAlign: "center",
                  marginBottom: "1rem",
                }}
              >
                {scannedCardData.confiscateCard && "ВЗЕМИ КАРТА"}
                <br />

                {scannedCardData.isScanned
                  ? !scannedCardData.cardSeqNumber
                    ? "Картата не е регистрирана"
                    : scannedCardData.isExpired
                    ? `Услугата "${scannedCardData.serviceName}" е изтекла`
                    : scannedCardData.isInactive
                    ? "Услугата на картата не е активна"
                    : `Няма услуга ${
                        scannedCardData.serviceName
                          ? `"${scannedCardData.serviceName}"`
                          : ""
                      }`
                  : ""}
              </h1>
            )}
            <h2 className="mb-1">
              {scannedCardData.hasEntryInLastPeriod && scannedCardData.valid
                ? "ПОВТОРНО ВЛИЗАНЕ"
                : ""}
            </h2>
            {(scannedCardData.ownerName || scannedCardData.cardSeqNumber) && (
              <React.Fragment>
                {scannedCardData.ownerName && (
                  <h2 className="mb-1">{`Притежател - ${scannedCardData.ownerName}`}</h2>
                )}
                {scannedCardData.cardSeqNumber && (
                  <h3 className="mb-05">
                    {`Номер на карта - #${scannedCardData.cardSeqNumber}`}
                  </h3>
                )}
                {scannedCardData.validTo && (
                  <h3 className="mb-05 text-center">
                    {`Дата на валидност - ${scannedCardData.validTo}`}
                  </h3>
                )}
                {scannedCardData.firstUse && (
                  <h3 className="mb-05 text-center">
                    {`Дата на активация - ${scannedCardData.firstUse}`}
                  </h3>
                )}
              </React.Fragment>
            )}
          </div>
          <div
            className="w-33 flex-col"
            style={{
              color: "black",
            }}
          >
            <h2 className="mtb-1 heading-with-border">Информация за карта</h2>
            {scannedCardData.companyName ? (
              <h3>Фирма: {scannedCardData.companyName}</h3>
            ) : (
              ""
            )}
            <h3 className="mtb-1 ml-1">
              {scannedCardData.totalEntries
                ? "Общо влизания с текущата карта - " +
                  scannedCardData.totalEntries
                : ""}
              <br />
              <br />
              {currentCardEntries.length ? "Последни влизания" : ""}
            </h3>
            {currentCardEntries.map((x, i) => {
              const date = new Date(x.timestamp);
              const day = date.toLocaleDateString("bg-BG", {
                day: "2-digit",
                month: "2-digit",
              });
              const time = date.toLocaleTimeString();
              return (
                <h3 className="mb-2 ta-l ml-1" key={i}>{`${
                  i + 1
                }. ${day} - ${time}`}</h3>
              );
            })}
          </div>
        </>
      </div>
    </div>
  );
};

createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </React.StrictMode>
);

function Clock() {
  const [time, setTime] = useState(new Date());

  useEffect(() => {
    const timerID = setInterval(() => {
      setTime(new Date());
    }, 1000);

    return () => clearInterval(timerID);
  }, []);

  return (
    <div className="clock">
      <h1 style={{ textAlign: "center" }}>{time.toLocaleTimeString()}</h1>
      <h4 style={{ textAlign: "center" }}>
        {new Date().toLocaleDateString("bg-BG")}
      </h4>
    </div>
  );
}
