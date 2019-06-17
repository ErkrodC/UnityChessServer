using System;

namespace UnityChessServer {
	internal class Program {
		private const int DefaultPlayersRequiredToStartGame = 2;
		
		public static void Main(string[] args) {
			Server server = new Server(DefaultPlayersRequiredToStartGame);
			server.Start();
			Console.ReadLine();
		}
	}
}