using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityChess;
using UnityChess.Networking;

namespace UnityChessServer {
	public class Server {
		private const int LocalPort = 23000;
		private const int DefaultBacklog = 4;
		private const int NumPlayersNeededToStartGame = 1;

		private readonly Socket listenSocket;
		private readonly List<Socket> playerSockets;

		public Server() {
			listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listenSocket.Bind(new IPEndPoint(IPAddress.Any, LocalPort));
			playerSockets = new List<Socket>(NumPlayersNeededToStartGame);
		}

		public void Start() {
			// 1) listen/wait for 2 players
			WaitForPlayers();
			
			// 2) start game
			Game game = new Game(Mode.HumanVsHuman, GameConditions.NormalStartingConditions);
			
			///////////////////////////////////////////////
			HalfMove latestHalfMove;
			
			do {
				bool moveExecuted;
				do {
					Movement move = GetMoveFromPlayer();	// 3) listen/wait for each player move	loop 1:
					moveExecuted = game.TryExecuteMove(move); // 4) execute move
					
					if (moveExecuted) {
						ReplicateGameState();
					} else {
						
					}
				} while (!moveExecuted); // 4a) wait for next move if received move was invalid

				// 5) replicate game state
				latestHalfMove = game.HalfMoveTimeline.Current;
			} while (!latestHalfMove.CausedCheckmate && !latestHalfMove.CausedStalemate); // 6) wait for next move if game not finished
		}

		private Movement GetMoveFromPlayer() {
			byte[] buffer = new byte[1024];
			playerSockets[0].Receive(buffer, 1024, SocketFlags.None);

			GCHandle pinnedArray = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			IntPtr ptr = pinnedArray.AddrOfPinnedObject();

			UnityChessDataPacket dataPacket = Marshal.PtrToStructure<UnityChessDataPacket>(ptr);
			pinnedArray.Free();

			switch (dataPacket.UserCommand) {
				case UserCommand.ExecuteMove:
					Square startSquare = new Square(dataPacket.byte0, dataPacket.byte1); // TODO send full move, not just start & end square (i.e. pack all move data into dataPacket fields a.k.a. serialization method)
					Square endSquare = new Square(dataPacket.byte2, dataPacket.byte3);
					
					// TODO move conversion of data packet to move into deserialization method
					
					Console.WriteLine($"Received move: {startSquare}->{endSquare}");
					return null;
				default:
					return null;
			}
		}

		private void WaitForPlayers() {
			while (true) {
				listenSocket.Listen(DefaultBacklog);
				Socket playerSocket = listenSocket.Accept();

				playerSockets.Add(playerSocket);
				Console.WriteLine("Player Connected");
				
				if (playerSockets.Count < NumPlayersNeededToStartGame) { continue; }
				break;
			}
		}

		private void ReplicateGameState() {
			foreach (Socket playerSocket in playerSockets) {
				// Send game state (e.g. serialized game instance)
			}
		}
	}
}