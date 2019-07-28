using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using UnityChess;

namespace UnityChessServer {
	public class Server {
		private const int LocalPort = 23000;
		private const int DefaultBacklog = 4;
		
		public ServerState State { get; }

		private readonly Socket listenSocket;
		private readonly List<IPEndPoint> playerRemoteEndpoints;

		private readonly int numPlayers;

		public Server(int numPlayers) {
			listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			listenSocket.Bind(new IPEndPoint(IPAddress.Any, LocalPort));

			this.numPlayers = numPlayers;
			playerRemoteEndpoints = new List<IPEndPoint>(numPlayers);
			
			State = ServerState.None;
		}

		public void Start() {
			// 1) listen/wait for 2 players
			WaitForPlayers();
			
			// 2) start game
			Game game = new Game(Mode.HumanVsHuman, GameConditions.NormalStartingConditions);
			
			///////////////////////////////////////////////
			HalfMove latestHalfMove;
			
			do {
				Movement move;
				
				do move = GetMoveFromPlayer();	// 3) listen/wait for each player move	loop 1:
				while (!game.TryExecuteMove(move));	// 4) execute move

				// 5) send updated game to each player	goto loop 1:
				latestHalfMove = game.HalfMoveTimeline.Current;
			} while (!latestHalfMove.CausedCheckmate && !latestHalfMove.CausedStalemate);
		}

		private Movement GetMoveFromPlayer() {
			byte[] buffer = new byte[1024];
			listenSocket.Receive(buffer, 1024, SocketFlags.None);
			
			BinaryFormatter bf = new BinaryFormatter();
			using(MemoryStream ms = new MemoryStream(buffer)) {
				Movement move = (Movement) bf.Deserialize(ms);
				Console.WriteLine($"Received move: {move}");
				return move;
			}
		}

		private void WaitForPlayers() {
			while (true) {
				listenSocket.Listen(DefaultBacklog);
				Socket playerSocket = listenSocket.Accept();

				playerRemoteEndpoints.Add(playerSocket.RemoteEndPoint as IPEndPoint);
				Console.WriteLine("Player Connected");
				if (playerRemoteEndpoints.Count < numPlayers) continue;
				
				Start();
				break;
			}
		}

		public enum ServerState {
			None,
			WaitingForPlayers,
			ListeningForMove,
			Finished
		}
	}
}