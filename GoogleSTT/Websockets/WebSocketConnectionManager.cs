﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GoogleSTT.GoogleAPI;
using log4net;

namespace GoogleSTT.Websockets
{
  public class WebSocketConnectionManager
  {
    private readonly ISpeechService _speechService;
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

    public WebSocketConnectionManager(ISpeechService speechService)
    {
      _speechService = speechService;
    }

    public WebSocket GetSocketById(string id)
    {
      return _sockets.FirstOrDefault(p => p.Key == id).Value;
    }

    public ConcurrentDictionary<string, WebSocket> GetAll()
    {
      return _sockets;
    }

    public string GetId(WebSocket socket)
    {
      return _sockets.FirstOrDefault(p => p.Value == socket).Key;
    }
    public void AddSocket(WebSocket socket, Action<string,string[]> processTranscripts)
    {
      var socketId = CreateConnectionId();
      _sockets.TryAdd(socketId, socket);

      _speechService.CreateSession(socketId, new GoogleSessionConfig(), processTranscripts);
    }

    public async Task RemoveSocket(string id, bool writeComplete)
    {
      WebSocket socket;
      _sockets.TryRemove(id, out socket);
      _speechService.CloseSession(id, writeComplete);

      await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure, 
        statusDescription: "Closed by the WebSocketManager", 
        cancellationToken: CancellationToken.None);
    }

    private string CreateConnectionId()
    {
      return Guid.NewGuid().ToString();
    }

  }
}
