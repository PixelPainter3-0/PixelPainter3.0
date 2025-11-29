import { defineStore } from 'pinia';
import { Pixel } from "@/entities/Pixel";
import { useToast } from "primevue/usetoast";
import { Vector2 } from "@/entities/Vector2";
import * as SignalR from "@microsoft/signalr";
import type Artist from '@/entities/Artist';
import { bus } from '@/bus/GridBus';

export const useSignalStore = defineStore('signal', {
    state: () => ({
        connection: null as SignalR.HubConnection | null,
        connected: false,
        toast: useToast()
    }),

    actions: {
        initConnection() {
            if (this.connection) {
                return this.connection; // already exists
            }

            this.connection = new SignalR.HubConnectionBuilder()
                .withUrl("http://localhost:7154/signalhub", {
                  skipNegotiation: true,
                  transport: SignalR.HttpTransportType.WebSockets
                })
                .build();

            // Bind events
            this.connection.onreconnected((id) => {
                console.log("Reconnected:", id);
                this.connected = true;
            });

            this.connection.onclose((error) => {
              if (error) {
                this.toast.add({
                  severity: "error",
                  summary: "Error",
                  detail: "You have disconnected!",
                  life: 3000
                });
                this.connected = false;
              }
            });

            this.connection.on(
              "TimeOuts", (dates: Date[]) => {
                bus.emit('timeouts', dates as Date[]);
              }
            );

            this.connection.on(
              "ReceivePixel",
              (layer: number, color: string, coord: Vector2) => {
                bus.emit('receivePixel', {layer, color, coord});
              }
            );

            this.connection.on(
              "GridConfig",
              async (canvasSize: number, backgroundColor: string, pixels: Pixel[][]) => {
                bus.emit('gridConfig', {canvasSize, backgroundColor, pixels});
              }
            )
            console.log("SignalR connection initialized.");
            return this.connection;
        },

        async start(artist: Artist) {
            if (!this.connection) this.initConnection();
            if (this.connection!.state === SignalR.HubConnectionState.Connected) {                this.joinGrid(artist);
                return;
            }
            try {
                console.log("Starting SignalR connection...");
                this.connection!
                  .start()
                  .then(() => {
                    this.connected = true;
                    console.log("SignalR connected:", this.connection!.connectionId);
                    this.joinGrid(artist);
                })
                .catch((err) => console.error("Error connecting to Hub:", err));
            } catch (err) {
                console.error("SignalR failed to start:", err);
            }
        },

        async joinGrid(artist: Artist) {
            console.log("Joining Grid for artist:", artist.name);
            this.connection!
                .invoke("JoinGrid", artist)
                .then(() => {
                  this.connected = true;
                })
                .catch((err) => {
                  this.toast.add({
                    severity: "error",
                    summary: "Error",
                    detail: err.toString().slice(err.toString().indexOf("HubException:")),
                    life: 4000
                  });
                  console.log("Error Joining Group:", err);
                  this.connection!.stop();
                });
        },

        async stop(artist: Artist) {
            if (this.connection &&
                this.connection.state === SignalR.HubConnectionState.Connected) {
                this.connection
                  .invoke("LeaveGrid", artist)
                  .then(() => {
                    this.connection!
                      .stop()
                      .then(() => {
                        console.log("Disconnected from Hub");
                        this.connected = false;
                      })
                      .catch((err) => console.error("Error Disconnecting:", err));
                  })
                  .catch((err) => console.error("Error Leaving Group:", err));
            }
        },
        async sendPixels(color: string, coord: Vector2, artistId: number) {
            if (this.connection &&
                this.connection.state === SignalR.HubConnectionState.Connected) {
                this.connection
                  .invoke("SendGridPixels", color, coord, artistId)
                  .catch((err) => console.error("Error Sending Pixels:", err));
            }
        }
    }
});
