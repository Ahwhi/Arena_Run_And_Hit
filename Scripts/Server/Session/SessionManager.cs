using System;
using System.Collections.Generic;
using System.Text;

namespace Server {
    class SessionManager {
        static SessionManager _session = new SessionManager();
        public static SessionManager Instance { get { return _session; } }

        int _sessionId = 0;
        Dictionary<int, ClientSession> _sessions = new Dictionary<int, ClientSession>();
        object _lock = new object();

        public ClientSession Generate() {
            lock (_lock) {
                int sessionId = ++_sessionId;

                ClientSession session = new ClientSession();
                session.SessionID = sessionId;
                SessionAccount account = new SessionAccount();
                session.Account = account;
                SessionGame game = new SessionGame();
                session.Game = game;
                _sessions.Add(sessionId, session);

                //Console.WriteLine($"Connected : {sessionId}");

                return session;
            }
        }

        public int Total() {
            lock (_lock) {
                return _sessions.Count;
            }
        }

        public ClientSession Find(int id) {
            lock (_lock) {
                ClientSession session = null;
                _sessions.TryGetValue(id, out session);
                return session;
            }
        }

        public void Remove(ClientSession session) {
            lock (_lock) {
                _sessions.Remove(session.SessionID);
            }
        }
    }
}
