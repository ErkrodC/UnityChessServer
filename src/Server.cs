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

		private Socket listenSocket;
		private IPEndPoint localEndPoint;
		private List<IPEndPoint> playerRemoteEndpoints;

		private int numPlayers;

		public Server(int numPlayers) {
			listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			localEndPoint = new IPEndPoint(IPAddress.Any, LocalPort);
			listenSocket.Bind(localEndPoint);

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
				return (Movement) bf.Deserialize(ms);
			}
		}

		private void WaitForPlayers() {
			listenSocket.Listen(DefaultBacklog);
			Socket playerSocket = listenSocket.Accept();
			
			playerRemoteEndpoints.Add(playerSocket.RemoteEndPoint as IPEndPoint);
			if (playerRemoteEndpoints.Count < numPlayers) WaitForPlayers();
			//else start game
		}

		public enum ServerState {
			None,
			WaitingForPlayers,
			ListeningForMove,
			Finished
		}
	}
}