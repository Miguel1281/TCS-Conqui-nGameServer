using ConquiánServidor.Contracts.DataContracts;

namespace ConquiánServidor.BusinessLogic.Interfaces
{
    public interface ILobbySessionManager
    {
        LobbySession GetLobbySession(string roomCode);
        LobbySession CreateLobby(string roomCode, PlayerDto host);
        PlayerDto AddPlayerToLobby(string roomCode, PlayerDto player);
        PlayerDto AddGuestToLobby(string roomCode);
        PlayerDto RemovePlayerFromLobby(string roomCode, int idPlayer);
        void RemoveLobby(string roomCode);
        void SetGamemode(string roomCode, int idGamemode);
        void BanPlayer(string roomCode, int idPlayer);
        string GetLobbyCodeForPlayer(int idPlayer);
    }
}