using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        List<StateMachine<T>> peers;
        int peerDispatchingDepth;

        /// <summary>
        /// Peer machines linked to this machine. Symmetric: if A sees B, B sees A.
        /// Null if no peers are linked.
        /// </summary>
        public IReadOnlyList<StateMachine<T>> Peers => peers?.AsReadOnly();

        /// <summary>
        /// Create a symmetric peer link. Static because the relationship is symmetric
        /// -- neither machine is privileged.
        /// </summary>
        public static void Link(StateMachine<T> a, StateMachine<T> b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (a == b)
                throw new InvalidOperationException("A machine cannot link to itself.");
            if (a.peerDispatchingDepth > 0 || b.peerDispatchingDepth > 0)
                throw new InvalidOperationException(
                    "Cannot Link/Unlink while peer event dispatch is in progress.");

            a.peers ??= new List<StateMachine<T>>();
            b.peers ??= new List<StateMachine<T>>();

            if (a.peers.Contains(b))
                throw new InvalidOperationException("Machines are already linked.");

            a.peers.Add(b);
            b.peers.Add(a);
        }

        /// <summary>
        /// Remove a symmetric peer link. Does NOT exit either machine's state --
        /// peers don't own each other's lifecycle.
        /// </summary>
        public static void Unlink(StateMachine<T> a, StateMachine<T> b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (a.peerDispatchingDepth > 0 || b.peerDispatchingDepth > 0)
                throw new InvalidOperationException(
                    "Cannot Link/Unlink while peer event dispatch is in progress.");

            bool removedFromA = a.peers != null && a.peers.Remove(b);
            bool removedFromB = b.peers != null && b.peers.Remove(a);

            if (!removedFromA || !removedFromB)
                throw new InvalidOperationException("Machines are not linked.");

            if (a.peers.Count == 0) a.peers = null;
            if (b.peers.Count == 0) b.peers = null;
        }

        int peerEventForwardingDepth;
        const int MaxPeerEventForwardingDepth = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SendEventToPeers<E>(E ev)
        {
            if (peers == null) return;
            if (peerEventForwardingDepth >= MaxPeerEventForwardingDepth)
            {
                LogFormat("Peer event forwarding depth limit reached ({0}).",
                          MaxPeerEventForwardingDepth);
                return;
            }
            peerDispatchingDepth++;
            peerEventForwardingDepth++;
            try
            {
                var forwarded = new ForwardedEvent<E>(this, ev);
                for (int i = 0; i < peers.Count; i++)
                    peers[i].ReceiveFromPeer(this, forwarded);
            }
            finally
            {
                peerEventForwardingDepth--;
                peerDispatchingDepth--;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void NotifyPeersOfStateChange(IState<T> from, IState<T> to)
        {
            if (peers == null) return;
            if (peerEventForwardingDepth >= MaxPeerEventForwardingDepth)
            {
                LogFormat("Peer event forwarding depth limit reached ({0}).",
                          MaxPeerEventForwardingDepth);
                return;
            }
            peerDispatchingDepth++;
            peerEventForwardingDepth++;
            try
            {
                var ev = new PeerStateChangedEvent(this, from, to);
                for (int i = 0; i < peers.Count; i++)
                    peers[i].ReceiveFromPeer(this, ev);
            }
            finally
            {
                peerEventForwardingDepth--;
                peerDispatchingDepth--;
            }
        }

        /// <summary>
        /// Receive from a peer. Delivers to own states only. NO re-broadcast.
        /// </summary>
        internal void ReceiveFromPeer<E>(StateMachine<T> source, E ev)
        {
            if (peerEventForwardingDepth >= MaxPeerEventForwardingDepth)
            {
                LogFormat("Peer event forwarding depth limit reached on receiver ({0}).",
                          MaxPeerEventForwardingDepth);
                return;
            }
            peerEventForwardingDepth++;
            try
            {
                SendEventToCurrentState(ev);
                SendEventToPopupStates(ev);
            }
            finally
            {
                peerEventForwardingDepth--;
            }
        }
    }
}
