using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace SilverWandererMarket.Market
{
    internal sealed class AuctionLogEntry
    {
        public long UtcTicks;
        public string Text;

        public AuctionLogEntry() { }

        public AuctionLogEntry(string text)
        {
            UtcTicks = DateTime.UtcNow.Ticks;
            Text = text ?? "";
        }

        public string Serialize()
        {
            return UtcTicks.ToString(CultureInfo.InvariantCulture) + "\t" + Esc(Text);
        }

        public static AuctionLogEntry Deserialize(string line)
        {
            if (string.IsNullOrEmpty(line))
                return null;
            int tab = line.IndexOf('\t');
            if (tab < 0)
                return new AuctionLogEntry(line);
            AuctionLogEntry e = new AuctionLogEntry();
            long t;
            long.TryParse(line.Substring(0, tab), NumberStyles.Integer, CultureInfo.InvariantCulture, out t);
            e.UtcTicks = t;
            e.Text = Unesc(line.Substring(tab + 1));
            return e;
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n");
        }

        private static string Unesc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\\\", "\\");
        }
    }

    internal sealed class AuctionState
    {
        public const string PlayerKey = "player";

        public WandererOffer Lot;
        public bool Settled;
        public bool Closed;
        public int HighBid;
        public string HighBidderKey = "";
        public string HighBidderName = "";
        public int PlayerBid;
        public long PlayerCooldownUntilUtcTicks;
        public long NextAiBidUtcTicks;
        public int PreviousHighBid;
        public string PreviousHighBidderKey = "";
        public string PreviousHighBidderName = "";
        /// <summary>Gold currently held for the live high bidder. Refunded on outbid; kept on win.</summary>
        public int EscrowAmount;
        public string EscrowBidderKey = "";
        public List<AuctionLogEntry> Log = new List<AuctionLogEntry>();

        public bool HasLot { get { return Lot != null; } }
        public bool PlayerIsHigh
        {
            get { return PlayerBid > 0 && HighBidderKey == SWMAuctionHooks.LocalBidderKey(); }
        }

        public void Clear()
        {
            Lot = null;
            Settled = false;
            Closed = false;
            HighBid = 0;
            HighBidderKey = "";
            HighBidderName = "";
            PlayerBid = 0;
            PlayerCooldownUntilUtcTicks = 0;
            NextAiBidUtcTicks = 0;
            PreviousHighBid = 0;
            PreviousHighBidderKey = "";
            PreviousHighBidderName = "";
            EscrowAmount = 0;
            EscrowBidderKey = "";
            Log = new List<AuctionLogEntry>();
        }

        public void AddLog(string text)
        {
            if (Log == null)
                Log = new List<AuctionLogEntry>();
            Log.Add(new AuctionLogEntry(text));
            if (Log.Count > 80)
                Log.RemoveAt(0);
        }

        public string Pack()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Settled ? "1" : "0").Append('\n');
            sb.Append(Closed ? "1" : "0").Append('\n');
            sb.Append(HighBid).Append('\n');
            sb.Append(Esc(HighBidderKey)).Append('\n');
            sb.Append(Esc(HighBidderName)).Append('\n');
            sb.Append(PlayerBid).Append('\n');
            sb.Append(PlayerCooldownUntilUtcTicks).Append('\n');
            sb.Append(NextAiBidUtcTicks).Append('\n');
            sb.Append(PreviousHighBid).Append('\n');
            sb.Append(Esc(PreviousHighBidderKey)).Append('\n');
            sb.Append(Esc(PreviousHighBidderName)).Append('\n');
            sb.Append(Lot != null ? Lot.Serialize() : "").Append('\n');
            sb.Append(EscrowAmount).Append('\n');
            sb.Append(Esc(EscrowBidderKey)).Append('\n');
            sb.Append("---LOG---\n");
            if (Log != null)
            {
                for (int i = 0; i < Log.Count; i++)
                {
                    if (Log[i] != null)
                        sb.Append(Log[i].Serialize()).Append('\n');
                }
            }
            return sb.ToString();
        }

        public void Unpack(string blob)
        {
            Clear();
            if (string.IsNullOrEmpty(blob))
                return;
            string[] lines = blob.Split('\n');
            if (lines.Length < 12)
                return;
            Settled = lines[0] == "1";
            Closed = lines[1] == "1";
            int.TryParse(lines[2], out HighBid);
            HighBidderKey = Unesc(lines[3]);
            HighBidderName = Unesc(lines[4]);
            int.TryParse(lines[5], out PlayerBid);
            long.TryParse(lines[6], out PlayerCooldownUntilUtcTicks);
            long.TryParse(lines[7], out NextAiBidUtcTicks);
            int.TryParse(lines[8], out PreviousHighBid);
            PreviousHighBidderKey = Unesc(lines[9]);
            PreviousHighBidderName = Unesc(lines[10]);
            if (!string.IsNullOrEmpty(lines[11]))
                Lot = WandererOffer.Deserialize(lines[11]);
            int i = 12;
            if (i < lines.Length && lines[i] != "---LOG---")
            {
                int.TryParse(lines[i], out EscrowAmount);
                i++;
                if (i < lines.Length && lines[i] != "---LOG---")
                {
                    EscrowBidderKey = Unesc(lines[i]);
                    i++;
                }
            }
            Log = new List<AuctionLogEntry>();
            bool inLog = false;
            for (; i < lines.Length; i++)
            {
                if (lines[i] == "---LOG---")
                {
                    inLog = true;
                    continue;
                }
                if (!inLog)
                    continue;
                AuctionLogEntry e = AuctionLogEntry.Deserialize(lines[i]);
                if (e != null && !string.IsNullOrEmpty(e.Text))
                    Log.Add(e);
            }
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n");
        }

        private static string Unesc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\t", "\t").Replace("\\n", "\n").Replace("\\\\", "\\");
        }
    }

    internal static class AuctionService
    {
        public static void StartNew(MarketState state)
        {
            if (state == null)
                return;
            if (state.Config == null)
                state.Config = MarketConfig.Load();
            if (state.Auction == null)
                state.Auction = new AuctionState();

            if (state.Auction.HasLot && !state.Auction.Settled)
                TrySettle(state, force: true);

            state.Auction.Clear();
            state.Auction.Lot = OfferGenerator.GenerateAuctionLot(state.Config, state.UsedIdentities);
            if (state.Auction.Lot != null)
                state.UsedIdentities.Add(state.Auction.Lot.IdentityKey);

            state.Auction.AddLog("The gavel rests. A hush falls over the booth...");
            if (state.Auction.Lot != null)
            {
                state.Auction.AddLog(state.Auction.Lot.DisplayName + " steps into the light, a "
                    + state.Auction.Lot.RoleTitle + " without peer on the slate.");
                state.Auction.AddLog("Opening call: " + state.Config.AuctionMinBid.ToString("N0") + " denars. Speak if you dare.");
            }
            else
                state.Auction.AddLog("No lot could be raised this hour. The auctioneer shrugs.");

            SWMAuctionHooks.Raise(SWMAuctionHookKind.LotStarted, state.Auction, "", "", 0, "", null);
            SWMLog.Info("SWMAuction", "StartNew lot=" + (state.Auction.Lot != null ? state.Auction.Lot.DisplayName : "none"));
            MarketState.Notify();
        }

        public static void Tick(MarketState state)
        {
            if (!SWMAuctionHooks.IsAuthoritative)
                return;
            if (state == null)
                return;
            if (state.Auction == null)
                state.Auction = new AuctionState();
            if (state.Config == null)
                state.Config = MarketConfig.Load();

            if (!state.Auction.HasLot)
                return;

            MigrateLegacyBidIntoEscrow(state);

            if (!state.Auction.Closed && !state.Auction.Settled)
            {
                TimeSpan untilRefresh = state.TimeUntilRefresh();
                int closeBefore = state.Config.AuctionCloseBeforeRefreshSeconds;
                if (closeBefore < 5)
                    closeBefore = 5;
                if (untilRefresh.TotalSeconds <= closeBefore)
                {
                    state.Auction.Closed = true;
                    state.Auction.AddLog("Final call! The auctioneer raises a hand. No further bids.");
                    SWMAuctionHooks.Raise(SWMAuctionHookKind.AuctionClosed, state.Auction, "", "", 0, "", null);
                    MarketState.Notify();
                }
            }

            if (state.Auction.Closed && !state.Auction.Settled)
                TrySettle(state, force: false);
        }

        public static string TryPlayerBid(MarketState state, int amount, out string error)
        {
            Hero hero = SWMAuctionHooks.LocalHero();
            string key = SWMAuctionHooks.LocalBidderKey();
            string name = hero != null && hero.Name != null ? hero.Name.ToString() : "You";
            return TryBid(state, key, name, hero, amount, true, out error);
        }

        public static string TryBid(MarketState state, string bidderKey, string bidderName, Hero hero, int amount, bool applyLocalCooldown, out string error)
        {
            error = null;
            if (state == null || state.Auction == null || state.Auction.Lot == null)
            {
                error = "No auction lot.";
                return null;
            }
            AuctionState a = state.Auction;
            MarketConfig cfg = state.Config ?? new MarketConfig();
            if (a.Settled || a.Closed)
            {
                error = "The auction is closed.";
                return null;
            }
            if (string.IsNullOrEmpty(bidderKey))
            {
                error = "No bidder.";
                return null;
            }

            bool ai = SWMAuctionHooks.IsSimulatedAiKey(bidderKey);
            if (!ai && hero == null)
            {
                error = "No player hero.";
                return null;
            }

            long now = DateTime.UtcNow.Ticks;
            if (applyLocalCooldown && SWMAuctionHooks.IsLocalBidder(bidderKey) && now < a.PlayerCooldownUntilUtcTicks)
            {
                int left = (int)Math.Ceiling(TimeSpan.FromTicks(a.PlayerCooldownUntilUtcTicks - now).TotalSeconds);
                if (left < 1) left = 1;
                error = "Hold your tongue. " + left + "s before you may bid again.";
                return null;
            }

            int minBid = cfg.AuctionMinBid;
            if (minBid < 1) minBid = 1000;
            int minRaise = cfg.AuctionMinRaise;
            if (minRaise < 1) minRaise = 1000;

            int need = a.HighBid <= 0 ? minBid : a.HighBid + minRaise;
            if (amount < need)
            {
                error = "Bid at least " + need.ToString("N0") + " denars.";
                return null;
            }
            if (amount < minBid)
            {
                error = "Minimum bid is " + minBid.ToString("N0") + " denars.";
                return null;
            }

            if (!ai)
            {
                string gate = SWMAuctionHooks.CompanionGate(hero);
                if (!string.IsNullOrEmpty(gate))
                {
                    error = gate;
                    return null;
                }
            }

            int alreadyHeld = (!ai && a.EscrowBidderKey == bidderKey) ? a.EscrowAmount : 0;
            int fromPurse = amount - alreadyHeld;
            if (fromPurse < 0)
            {
                error = "Bid must exceed your held stake.";
                return null;
            }

            if (!ai)
            {
                string debitErr = SWMAuctionEscrow.TryTake(hero, fromPurse);
                if (debitErr != null)
                {
                    error = debitErr;
                    return null;
                }
                SWMAuctionHooks.Raise(SWMAuctionHookKind.EscrowTaken, a, bidderKey, bidderName, fromPurse, "", null);
            }

            if (a.EscrowAmount > 0 && a.EscrowBidderKey != bidderKey)
                SWMAuctionEscrow.RefundHeld(a);

            PushPrevious(a);
            a.HighBid = amount;
            a.HighBidderKey = bidderKey;
            a.HighBidderName = bidderName ?? "";
            if (ai)
            {
                a.EscrowAmount = 0;
                a.EscrowBidderKey = "";
            }
            else
            {
                a.EscrowAmount = amount;
                a.EscrowBidderKey = bidderKey;
            }

            if (SWMAuctionHooks.IsLocalBidder(bidderKey))
            {
                a.PlayerBid = amount;
                if (applyLocalCooldown)
                    a.PlayerCooldownUntilUtcTicks = DateTime.UtcNow.AddSeconds(cfg.AuctionBidCooldownSeconds).Ticks;
            }

            a.AddLog((bidderName ?? "A bidder") + " bids " + amount.ToString("N0") + " denars!");
            SWMAuctionHooks.Raise(SWMAuctionHookKind.BidPlaced, a, bidderKey, bidderName, amount, "", null);
            MarketState.Notify();
            return "Bid placed: " + amount.ToString("N0");
        }

        /// <summary>
        /// Old saves stored a standing bid without taking gold. Capture it into escrow once, or drop it.
        /// Live escrow already left the purse — do not void those.
        /// </summary>
        public static void MigrateLegacyBidIntoEscrow(MarketState state)
        {
            AuctionState a = state != null ? state.Auction : null;
            if (a == null || a.Settled || a.PlayerBid <= 0)
                return;
            if (a.EscrowAmount > 0)
                return;

            string localKey = SWMAuctionHooks.LocalBidderKey();
            if (a.HighBidderKey != localKey)
            {
                a.PlayerBid = 0;
                return;
            }

            Hero hero = SWMAuctionHooks.LocalHero();
            string err = SWMAuctionEscrow.TryTake(hero, a.PlayerBid);
            if (err == null)
            {
                a.EscrowAmount = a.PlayerBid;
                a.EscrowBidderKey = localKey;
                a.AddLog("The auctioneer takes " + a.PlayerBid.ToString("N0") + " denars into the bowl.");
                MarketState.Notify();
                return;
            }

            int lost = a.PlayerBid;
            a.PlayerBid = 0;
            a.HighBid = 0;
            a.HighBidderKey = "";
            a.HighBidderName = "";
            a.AddLog("Your purse runs light. The auctioneer strikes your mark of "
                + lost.ToString("N0") + ". Bid voided.");
            MarketState.Notify();
        }

        public static void TrySettle(MarketState state, bool force)
        {
            AuctionState a = state.Auction;
            if (a == null || a.Lot == null || a.Settled)
                return;
            if (!force && !a.Closed)
                return;

            a.Closed = true;
            MigrateLegacyBidIntoEscrow(state);

            if (a.HighBid <= 0 || string.IsNullOrEmpty(a.HighBidderKey))
            {
                SWMAuctionEscrow.RefundHeld(a);
                a.AddLog("No sale. The lot is withdrawn into shadow.");
                a.PlayerBid = 0;
                a.Settled = true;
                SWMAuctionHooks.Raise(SWMAuctionHookKind.SettleNoSale, a, "", "", 0, "", null);
                MarketState.Notify();
                return;
            }

            if (SWMAuctionHooks.IsSimulatedAiKey(a.HighBidderKey))
            {
                SWMAuctionEscrow.RefundHeld(a);
                a.AddLog("Sold! To " + a.HighBidderName + " for " + a.HighBid.ToString("N0") + " denars. The gavel falls!");
                a.PlayerBid = 0;
                a.Settled = true;
                SWMAuctionHooks.Raise(SWMAuctionHookKind.SettleWin, a, a.HighBidderKey, a.HighBidderName, a.HighBid, "", null);
                MarketState.Notify();
                return;
            }

            Hero winner = SWMAuctionHooks.ResolveHeroOrNull(a.HighBidderKey);
            string gate = SWMAuctionHooks.CompanionGate(winner);
            if (!string.IsNullOrEmpty(gate))
            {
                a.AddLog("The winner cannot claim the prize (" + gate + "). Stake returned. No sale.");
                SWMAuctionEscrow.RefundHeld(a);
                a.PlayerBid = 0;
                a.Settled = true;
                SWMAuctionHooks.Raise(SWMAuctionHookKind.SettleNoSale, a, a.HighBidderKey, a.HighBidderName, a.HighBid, "", gate);
                MarketState.Notify();
                return;
            }

            int paid = a.HighBid;
            string hired = DeliverLot(a.Lot, winner, paid, out string err);
            if (hired == null)
            {
                a.AddLog("Hire failed: " + (err ?? "unknown") + ". Stake returned. Lot withdrawn.");
                SWMAuctionEscrow.RefundHeld(a);
                a.PlayerBid = 0;
                a.Settled = true;
                SWMAuctionHooks.Raise(SWMAuctionHookKind.SettleNoSale, a, a.HighBidderKey, a.HighBidderName, paid, "", err);
                MarketState.Notify();
                return;
            }

            SWMAuctionEscrow.CaptureHeld(a);
            a.PlayerBid = 0;
            a.Settled = true;
            a.AddLog("Sold! To " + a.HighBidderName + ", " + hired + ", for " + paid.ToString("N0") + " denars. The gavel falls!");
            if (SWMAuctionHooks.IsLocalBidder(a.HighBidderKey))
                InformationManager.DisplayMessage(new InformationMessage("Auction won: " + hired + " for " + paid.ToString("N0") + "."));
            SWMAuctionHooks.Raise(SWMAuctionHookKind.SettleWin, a, a.HighBidderKey, a.HighBidderName, paid, "", hired);
            SWMMarketHooks.Raise(SWMMarketHookKind.CompanionHired, a.HighBidderKey, a.HighBidderName, a.Lot != null ? a.Lot.Id : "", hired, paid, null);
            MarketState.Notify();
        }

        private static string DeliverLot(WandererOffer lot, Hero winner, int paid, out string error)
        {
            return SWMMarketApi.DeliverPaidOffer(lot, winner, paid, out error);
        }

        private static void PushPrevious(AuctionState a)
        {
            if (a.HighBid > 0 && !string.IsNullOrEmpty(a.HighBidderKey))
            {
                a.PreviousHighBid = a.HighBid;
                a.PreviousHighBidderKey = a.HighBidderKey;
                a.PreviousHighBidderName = a.HighBidderName;
            }
        }
    }
}
