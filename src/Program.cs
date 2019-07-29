using System;

namespace UnityChessServer {
	internal class Program {
		public static void Main(string[] args) {
			Server server = new Server();
			server.Start();
			Console.ReadLine();
		}
	}
}