using System;
using System.Collections.Generic;
using SilverWandererMarket.Heroes;
using SilverWandererMarket.Market;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace SilverWandererMarket.UI
{
    internal sealed class SWMSkillRowVM : ViewModel
    {
        private string _nameText;
        private string _valueText;
        private bool _isHighlight;

        public SWMSkillRowVM(string name, int value, bool highlight)
        {
            _nameText = name ?? "";
            _valueText = value.ToString();
            _isHighlight = highlight;
        }

        [DataSourceProperty]
        public string NameText
        {
            get { return _nameText; }
            set { if (_nameText != value) { _nameText = value; OnPropertyChangedWithValue(value, "NameText"); } }
        }

        [DataSourceProperty]
        public string ValueText
        {
            get { return _valueText; }
            set { if (_valueText != value) { _valueText = value; OnPropertyChangedWithValue(value, "ValueText"); } }
        }

        [DataSourceProperty]
        public bool IsHighlight
        {
            get { return _isHighlight; }
            set { if (_isHighlight != value) { _isHighlight = value; OnPropertyChangedWithValue(value, "IsHighlight"); } }
        }
    }

    internal sealed class SWMWandererRowVM : ViewModel
    {
        private readonly WandererOffer _offer;
        private readonly SWMMarketVM _parent;
        private string _nameText;
        private string _roleText;
        private string _metaText;
        private string _skill1Text;
        private string _skill2Text;
        private string _skill3Text;
        private string _priceText;
        private bool _canBuy;
        private bool _isSelected;

        public SWMWandererRowVM(WandererOffer offer, SWMMarketVM parent)
        {
            _offer = offer;
            _parent = parent;
            _nameText = offer != null ? offer.DisplayName : "";
            _roleText = offer != null ? offer.RoleTitle : "";
            _metaText = offer != null
                ? (TitleCase(offer.CultureId) + " · " + offer.Age + (offer.IsFemale ? " · F" : " · M"))
                : "";
            _priceText = offer != null ? offer.Price.ToString("N0") : "";
            FillTopSkills(offer);
            RefreshCanBuy();
        }

        public WandererOffer Offer { get { return _offer; } }

        [DataSourceProperty]
        public string NameText
        {
            get { return _nameText; }
            set { if (_nameText != value) { _nameText = value; OnPropertyChangedWithValue(value, "NameText"); } }
        }

        [DataSourceProperty]
        public string RoleText
        {
            get { return _roleText; }
            set { if (_roleText != value) { _roleText = value; OnPropertyChangedWithValue(value, "RoleText"); } }
        }

        [DataSourceProperty]
        public string MetaText
        {
            get { return _metaText; }
            set { if (_metaText != value) { _metaText = value; OnPropertyChangedWithValue(value, "MetaText"); } }
        }

        [DataSourceProperty]
        public string Skill1Text
        {
            get { return _skill1Text; }
            set { if (_skill1Text != value) { _skill1Text = value; OnPropertyChangedWithValue(value, "Skill1Text"); } }
        }

        [DataSourceProperty]
        public string Skill2Text
        {
            get { return _skill2Text; }
            set { if (_skill2Text != value) { _skill2Text = value; OnPropertyChangedWithValue(value, "Skill2Text"); } }
        }

        [DataSourceProperty]
        public string Skill3Text
        {
            get { return _skill3Text; }
            set { if (_skill3Text != value) { _skill3Text = value; OnPropertyChangedWithValue(value, "Skill3Text"); } }
        }

        [DataSourceProperty]
        public string PriceText
        {
            get { return _priceText; }
            set { if (_priceText != value) { _priceText = value; OnPropertyChangedWithValue(value, "PriceText"); } }
        }

        [DataSourceProperty]
        public bool CanBuy
        {
            get { return _canBuy; }
            set { if (_canBuy != value) { _canBuy = value; OnPropertyChangedWithValue(value, "CanBuy"); } }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get { return _isSelected; }
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChangedWithValue(value, "IsSelected"); } }
        }

        public void RefreshCanBuy()
        {
            string reason;
            CanBuy = SWMMarketVM.CanHire(_offer, out reason);
        }

        public void ExecuteBuy()
        {
            if (_parent != null)
                _parent.TryBuy(_offer);
        }

        public void ExecuteSelect()
        {
            if (_parent != null)
                _parent.ShowDetails(_offer);
        }

        public void ExecuteDetails()
        {
            ExecuteSelect();
        }

        private void FillTopSkills(WandererOffer offer)
        {
            _skill1Text = "";
            _skill2Text = "";
            _skill3Text = "";
            if (offer == null || offer.Skills == null || offer.Skills.Count == 0)
                return;
            List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>(offer.Skills);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (list.Count > 0) _skill1Text = ShortSkill(list[0].Key) + " " + list[0].Value;
            if (list.Count > 1) _skill2Text = ShortSkill(list[1].Key) + " " + list[1].Value;
            if (list.Count > 2) _skill3Text = ShortSkill(list[2].Key) + " " + list[2].Value;
        }

        private static string ShortSkill(string skill)
        {
            if (skill == "OneHanded") return "1H";
            if (skill == "TwoHanded") return "2H";
            if (skill == "Polearm") return "Pole";
            if (skill == "Crossbow") return "Xbow";
            if (skill == "Athletics") return "Ath";
            if (skill == "Engineering") return "Eng";
            if (skill == "Leadership") return "Lead";
            if (skill == "Medicine") return "Med";
            if (skill == "Scouting") return "Scout";
            return skill;
        }

        private static string TitleCase(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }

    internal sealed class SWMLogLineVM : ViewModel
    {
        private string _text = "";

        public SWMLogLineVM(string text)
        {
            _text = text ?? "";
        }

        [DataSourceProperty]
        public string Text
        {
            get { return _text; }
            set { if (_text != value) { _text = value; OnPropertyChangedWithValue(value, "Text"); } }
        }
    }

    internal sealed class SWMMarketVM : ViewModel
    {
        private enum SortMode
        {
            Name = 0,
            PriceAsc = 1,
            PriceDesc = 2,
            Role = 3
        }

        private readonly List<WandererOffer> _allOffers = new List<WandererOffer>();
        private MBBindingList<SWMWandererRowVM> _rows = new MBBindingList<SWMWandererRowVM>();
        private MBBindingList<SWMSkillRowVM> _detailsPrimarySkills = new MBBindingList<SWMSkillRowVM>();
        private MBBindingList<SWMSkillRowVM> _detailsAllSkills = new MBBindingList<SWMSkillRowVM>();
        private MBBindingList<SWMSkillRowVM> _auctionPrimarySkills = new MBBindingList<SWMSkillRowVM>();
        private MBBindingList<SWMSkillRowVM> _auctionAllSkills = new MBBindingList<SWMSkillRowVM>();
        private MBBindingList<SWMLogLineVM> _auctionLog = new MBBindingList<SWMLogLineVM>();
        private string _titleText = "Wanderer Market";
        private string _goldText = "";
        private string _timerText = "";
        private string _statusText = "";
        private string _companionCountText = "";
        private string _stockCountText = "";
        private string _sortHintText = "";
        private SortMode _sortMode = SortMode.Name;
        private bool _isDetailsOpen;
        private string _detailsName = "";
        private bool _isCharacterViewOpen;
        private CharacterViewModel _detailsCharacter;
        private CharacterViewModel _auctionCharacter;
        private string _detailsRole = "";
        private string _detailsCulture = "";
        private string _detailsAge = "";
        private string _detailsGender = "";
        private string _detailsPriceValue = "";
        private string _detailsCannotBuyReason = "";
        private string _detailsQuality = "";
        private bool _detailsCanBuy;
        private bool _hasSelection;
        private WandererOffer _detailsOffer;
        private bool _isMarketTab = true;
        private bool _isAuctionTab;
        private string _auctionName = "";
        private string _auctionRole = "";
        private string _auctionCulture = "";
        private string _auctionAge = "";
        private string _auctionGender = "";
        private string _auctionQuality = "Auction Prize";
        private string _auctionHighBidText = "-";
        private string _auctionHighBidderText = "No bids yet";
        private string _auctionTimerText = "";
        private string _auctionStatusText = "";
        private string _auctionPlayerBidText = "";
        private string _auctionCooldownText = "";
        private string _auctionMinNextBidText = "";
        private string _bidInputText = "1000";
        private string _auctionBidError = "";
        private bool _auctionHasLot;
        private bool _auctionCanBid;
        private bool _auctionClosed;
        private string _auctionLotId = "";

        public SWMMarketVM()
        {
            Reload();
            MarketState.Changed += OnStockChanged;
        }

        public override void OnFinalize()
        {
            MarketState.Changed -= OnStockChanged;
            base.OnFinalize();
        }

        private void OnStockChanged()
        {
            MarketState state = MarketState.Ensure();
            bool stockChanged = false;
            int offerCount = state.Offers != null ? state.Offers.Count : 0;
            if (offerCount != _allOffers.Count)
                stockChanged = true;
            else if (state.Offers != null)
            {
                for (int i = 0; i < state.Offers.Count; i++)
                {
                    if (_allOffers[i] == null || state.Offers[i] == null || _allOffers[i].Id != state.Offers[i].Id)
                    {
                        stockChanged = true;
                        break;
                    }
                }
            }
            if (stockChanged)
            {
                Reload();
                return;
            }
            string lotId = state.Auction != null && state.Auction.Lot != null ? state.Auction.Lot.Id : "";
            if (_auctionLotId != lotId)
                RefreshAuctionLot(state);
            RefreshChrome();
        }

        public void Reload()
        {
            MarketState state = MarketState.Ensure();
            state.EnsureStock();
            _allOffers.Clear();
            if (state.Offers != null)
            {
                for (int i = 0; i < state.Offers.Count; i++)
                    _allOffers.Add(state.Offers[i]);
            }
            RebuildVisibleRows();
            RefreshAuctionLot(state);
            RefreshChrome();
        }

        public void Tick()
        {
            RefreshChrome();
        }

        public void ExecuteShowMarket()
        {
            IsMarketTab = true;
            IsAuctionTab = false;
            TitleText = "Wanderer Market";
        }

        public void ExecuteShowAuction()
        {
            IsMarketTab = false;
            IsAuctionTab = true;
            TitleText = "Wanderer Auction";
            RefreshChrome();
        }

        public void ExecuteBidPlus1k()
        {
            AdjustBidInput(1000);
        }

        public void ExecuteBidPlus5k()
        {
            AdjustBidInput(5000);
        }

        public void ExecuteBidPlus10k()
        {
            AdjustBidInput(10000);
        }

        public void ExecutePlaceBid()
        {
            MarketState state = MarketState.Ensure();
            int amount;
            if (!TryParseBid(BidInputText, out amount))
            {
                AuctionBidError = "Enter a valid bid amount.";
                return;
            }
            if (SWMMarketHooks.TrySendBidRequest != null && SWMMarketHooks.TrySendBidRequest(amount))
            {
                SWMLog.Info("SWMAuction", "TrySendBidRequest intercepted amount=" + amount + " key=" + SWMMarketHooks.LocalPlayerKey());
                AuctionBidError = "";
                return;
            }
            string err;
            string ok = SWMAuctionApi.TryPlaceBid(amount, out err);
            if (ok == null)
            {
                AuctionBidError = err ?? "Bid failed.";
                RefreshChrome();
                return;
            }
            AuctionBidError = "";
            StatusText = ok;
            // Suggest next raise for convenience.
            MarketConfig cfg = state.Config ?? new MarketConfig();
            BidInputText = (amount + Math.Max(1, cfg.AuctionMinRaise)).ToString();
            RefreshChrome();
        }

        private void AdjustBidInput(int delta)
        {
            int amount;
            if (!TryParseBid(BidInputText, out amount))
                amount = 0;
            amount += delta;
            if (amount < 0)
                amount = 0;
            BidInputText = amount.ToString();
        }

        private static bool TryParseBid(string text, out int amount)
        {
            amount = 0;
            if (string.IsNullOrEmpty(text))
                return false;
            string cleaned = text.Replace(",", "").Replace(" ", "").Trim();
            return int.TryParse(cleaned, out amount) && amount > 0;
        }

        public void ExecuteSortByName()
        {
            _sortMode = SortMode.Name;
            RebuildVisibleRows();
            RefreshChrome();
        }

        public void ExecuteSortByPrice()
        {
            if (_sortMode == SortMode.PriceAsc)
                _sortMode = SortMode.PriceDesc;
            else
                _sortMode = SortMode.PriceAsc;
            RebuildVisibleRows();
            RefreshChrome();
        }

        public void ExecuteSortByRole()
        {
            _sortMode = SortMode.Role;
            RebuildVisibleRows();
            RefreshChrome();
        }

        private void RebuildVisibleRows()
        {
            string keepId = _detailsOffer != null ? _detailsOffer.Id : null;
            List<WandererOffer> sorted = new List<WandererOffer>(_allOffers);
            sorted.Sort(CompareOffers);

            _rows.Clear();
            for (int i = 0; i < sorted.Count; i++)
                _rows.Add(new SWMWandererRowVM(sorted[i], this));
            Rows = _rows;

            SortHintText = SortModeLabel(_sortMode);
            StockCountText = _allOffers.Count.ToString();

            WandererOffer next = null;
            if (!string.IsNullOrEmpty(keepId))
            {
                for (int i = 0; i < sorted.Count; i++)
                {
                    if (sorted[i] != null && sorted[i].Id == keepId)
                    {
                        next = sorted[i];
                        break;
                    }
                }
            }
            if (next == null && sorted.Count > 0)
                next = sorted[0];
            if (next != null)
                ShowDetails(next);
            else
                ClearSelection();
        }

        private int CompareOffers(WandererOffer a, WandererOffer b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            switch (_sortMode)
            {
                case SortMode.PriceAsc:
                    return a.Price.CompareTo(b.Price);
                case SortMode.PriceDesc:
                    return b.Price.CompareTo(a.Price);
                case SortMode.Role:
                    {
                        int role = string.Compare(a.RoleTitle, b.RoleTitle, StringComparison.OrdinalIgnoreCase);
                        if (role != 0) return role;
                        return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
                    }
                default:
                    return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string SortModeLabel(SortMode mode)
        {
            switch (mode)
            {
                case SortMode.PriceAsc: return "Sorted by price ↑";
                case SortMode.PriceDesc: return "Sorted by price ↓";
                case SortMode.Role: return "Sorted by role";
                default: return "Sorted by name";
            }
        }

        private void RefreshChrome()
        {
            MarketState state = MarketState.Ensure();
            int gold = 0;
            Hero local = SWMMarketHooks.LocalHero();
            if (local != null)
                gold = local.Gold;
            GoldText = gold.ToString("N0");
            int have = 0;
            int cap = 0;
            Clan clan = local != null ? local.Clan : Clan.PlayerClan;
            if (clan != null)
            {
                have = clan.Companions.Count;
                cap = clan.CompanionLimit;
            }
            CompanionCountText = have + " / " + cap;
            TimeSpan left = state.TimeUntilRefresh();
            if (left.TotalSeconds <= 0)
                TimerText = "soon";
            else if (left.TotalHours >= 1)
                TimerText = ((int)left.TotalHours) + "h " + left.Minutes.ToString("00") + "m";
            else
                TimerText = left.Minutes.ToString("00") + ":" + left.Seconds.ToString("00");

            StockCountText = _allOffers.Count.ToString();
            StatusText = string.IsNullOrEmpty(state.StatusMessage) ? "" : state.StatusMessage;
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].RefreshCanBuy();
            if (_detailsOffer != null)
            {
                string reason;
                DetailsCanBuy = CanHire(_detailsOffer, out reason);
                DetailsCannotBuyReason = DetailsCanBuy ? "" : reason;
            }
            RefreshAuctionChrome(state);
        }

        /// <summary>
        /// Per-lot data: skills and the 3D model. Called only when the lot actually changes, since
        /// rebuilding the character model on every chrome tick would restart the tableau constantly.
        /// </summary>
        private void RefreshAuctionLot(MarketState state)
        {
            WandererOffer current = state != null && state.Auction != null ? state.Auction.Lot : null;
            _auctionLotId = current != null ? current.Id : "";
            AuctionCharacter = current != null ? WandererAppearance.BuildPreview(current) : null;
            RefreshAuctionLotSkills(state);
        }

        private void RefreshAuctionLotSkills(MarketState state)
        {
            _auctionPrimarySkills.Clear();
            _auctionAllSkills.Clear();
            WandererOffer lot = state != null && state.Auction != null ? state.Auction.Lot : null;
            if (lot == null || lot.Skills == null)
            {
                AuctionPrimarySkills = _auctionPrimarySkills;
                AuctionAllSkills = _auctionAllSkills;
                return;
            }
            List<KeyValuePair<string, int>> ranked = new List<KeyValuePair<string, int>>(lot.Skills);
            ranked.Sort((a, b) => b.Value.CompareTo(a.Value));
            HashSet<string> top = new HashSet<string>();
            int primaryCount = ranked.Count < 5 ? ranked.Count : 5;
            for (int i = 0; i < primaryCount; i++)
            {
                top.Add(ranked[i].Key);
                _auctionPrimarySkills.Add(new SWMSkillRowVM(ranked[i].Key, ranked[i].Value, true));
            }
            // Untrained skills are left out entirely, the way a vanilla wanderer sheet reads.
            for (int i = 0; i < ranked.Count; i++)
            {
                if (ranked[i].Value <= 0)
                    continue;
                _auctionAllSkills.Add(new SWMSkillRowVM(ranked[i].Key, ranked[i].Value, top.Contains(ranked[i].Key)));
            }
            AuctionPrimarySkills = _auctionPrimarySkills;
            AuctionAllSkills = _auctionAllSkills;
        }

        private void RefreshAuctionChrome(MarketState state)
        {
            AuctionState a = state.Auction;
            MarketConfig cfg = state.Config ?? new MarketConfig();
            if (a == null || a.Lot == null)
            {
                AuctionHasLot = false;
                AuctionName = "";
                AuctionStatusText = "No lot this hour.";
                AuctionCanBid = false;
                RebuildAuctionLog(a);
                return;
            }

            AuctionHasLot = true;
            AuctionName = a.Lot.DisplayName;
            AuctionRole = a.Lot.RoleTitle;
            AuctionCulture = TitleCulture(a.Lot.CultureId);
            AuctionAge = a.Lot.Age.ToString();
            AuctionGender = a.Lot.IsFemale ? "Female" : "Male";
            AuctionQuality = TitleQuality(a.Lot.QualityId);
            AuctionClosed = a.Closed || a.Settled;

            if (a.HighBid > 0)
            {
                AuctionHighBidText = a.HighBid.ToString("N0");
                AuctionHighBidderText = string.IsNullOrEmpty(a.HighBidderName) ? "Unknown" : a.HighBidderName;
            }
            else
            {
                AuctionHighBidText = "-";
                AuctionHighBidderText = "No bids yet";
            }

            if (a.Settled)
            {
                AuctionTimerText = "Sold";
                AuctionStatusText = a.HighBid > 0
                    ? ("Gavel down. Sold to " + a.HighBidderName + ".")
                    : "No sale this hour.";
            }
            else if (a.Closed)
            {
                AuctionTimerText = "Closed";
                AuctionStatusText = "Bidding closed. Awaiting the gavel...";
            }
            else
            {
                TimeSpan left = state.TimeUntilAuctionClose();
                if (left.TotalSeconds <= 0)
                    AuctionTimerText = "Closing";
                else if (left.TotalHours >= 1)
                    AuctionTimerText = ((int)left.TotalHours) + "h " + left.Minutes.ToString("00") + "m";
                else
                    AuctionTimerText = left.Minutes.ToString("00") + ":" + left.Seconds.ToString("00");
                AuctionStatusText = "Live auction. Raises welcome.";
            }

            if (a.PlayerBid > 0)
                AuctionPlayerBidText = "Your standing bid: " + a.PlayerBid.ToString("N0");
            else
                AuctionPlayerBidText = "You have no standing bid.";

            long now = DateTime.UtcNow.Ticks;
            if (now < a.PlayerCooldownUntilUtcTicks)
            {
                int sec = (int)Math.Ceiling(TimeSpan.FromTicks(a.PlayerCooldownUntilUtcTicks - now).TotalSeconds);
                if (sec < 1) sec = 1;
                AuctionCooldownText = "Bid cooldown: " + sec + "s";
            }
            else
                AuctionCooldownText = "";

            int minNext = a.HighBid <= 0 ? cfg.AuctionMinBid : a.HighBid + cfg.AuctionMinRaise;
            AuctionMinNextBidText = "Next bid at least " + minNext.ToString("N0");

            string gate;
            bool room = CanHireAuction(a.Lot, out gate);
            AuctionCanBid = !a.Closed && !a.Settled && room;
            if (!AuctionCanBid && !a.Closed && !a.Settled && !string.IsNullOrEmpty(gate))
                AuctionStatusText = gate;

            RebuildAuctionLog(a);
        }

        private static bool CanHireAuction(WandererOffer offer, out string reason)
        {
            reason = "";
            if (offer == null)
            {
                reason = "No lot.";
                return false;
            }
            reason = SWMAuctionHooks.CompanionGate(SWMAuctionHooks.LocalHero());
            return string.IsNullOrEmpty(reason);
        }

        private void RebuildAuctionLog(AuctionState a)
        {
            _auctionLog.Clear();
            if (a != null && a.Log != null)
            {
                // Newest at top for reading the action.
                for (int i = a.Log.Count - 1; i >= 0; i--)
                {
                    if (a.Log[i] != null && !string.IsNullOrEmpty(a.Log[i].Text))
                        _auctionLog.Add(new SWMLogLineVM(a.Log[i].Text));
                }
            }
            AuctionLog = _auctionLog;
        }

        public void TryBuy(WandererOffer offer)
        {
            if (offer == null)
                return;
            if (SWMMarketHooks.TrySendBuyRequest != null && SWMMarketHooks.TrySendBuyRequest(offer.Id))
            {
                SWMLog.Info("SWMMarket", "TrySendBuyRequest intercepted offer=" + offer.Id + " key=" + SWMMarketHooks.LocalPlayerKey());
                return;
            }
            string reason;
            if (!CanHire(offer, out reason))
            {
                MarketState.Ensure().StatusMessage = reason;
                RefreshChrome();
                return;
            }
            string err;
            string hired = SWMMarketApi.TryBuy(offer.Id, out err);
            if (hired == null)
            {
                MarketState.Ensure().StatusMessage = err ?? "Hire failed.";
                RefreshChrome();
                InformationManager.DisplayMessage(new InformationMessage(err ?? "Hire failed."));
                return;
            }
            MarketState.Ensure().StatusMessage = hired + " joined the party.";
            InformationManager.DisplayMessage(new InformationMessage(hired + " joined your party."));
            Reload();
        }

        public void ShowDetails(WandererOffer offer)
        {
            if (offer == null)
            {
                ClearSelection();
                return;
            }
            _detailsOffer = offer;
            // A stale model from the previous pick must not linger behind the overlay.
            ExecuteCloseCharacterView();
            DetailsName = offer.DisplayName;
            DetailsRole = offer.RoleTitle;
            DetailsCulture = TitleCulture(offer.CultureId);
            DetailsAge = offer.Age.ToString();
            DetailsGender = offer.IsFemale ? "Female" : "Male";
            DetailsQuality = TitleQuality(offer.QualityId);
            DetailsPriceValue = offer.Price.ToString("N0");
            BuildDetailsSkills(offer);
            string reason;
            DetailsCanBuy = CanHire(offer, out reason);
            DetailsCannotBuyReason = DetailsCanBuy ? "" : reason;
            HasSelection = true;
            IsDetailsOpen = true;
            SyncRowSelection(offer.Id);
        }

        private void SyncRowSelection(string offerId)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                SWMWandererRowVM row = _rows[i];
                if (row == null)
                    continue;
                row.IsSelected = row.Offer != null && row.Offer.Id == offerId;
            }
        }

        private void ClearSelection()
        {
            _detailsOffer = null;
            ExecuteCloseCharacterView();
            HasSelection = false;
            IsDetailsOpen = false;
            DetailsCannotBuyReason = "";
            DetailsQuality = "";
            _detailsPrimarySkills.Clear();
            _detailsAllSkills.Clear();
            SyncRowSelection(null);
        }

        private void BuildDetailsSkills(WandererOffer offer)
        {
            _detailsPrimarySkills.Clear();
            _detailsAllSkills.Clear();
            if (offer == null || offer.Skills == null)
            {
                DetailsPrimarySkills = _detailsPrimarySkills;
                DetailsAllSkills = _detailsAllSkills;
                return;
            }

            List<KeyValuePair<string, int>> ranked = new List<KeyValuePair<string, int>>(offer.Skills);
            ranked.Sort((a, b) => b.Value.CompareTo(a.Value));
            HashSet<string> top = new HashSet<string>();
            int primaryCount = ranked.Count < 5 ? ranked.Count : 5;
            for (int i = 0; i < primaryCount; i++)
            {
                top.Add(ranked[i].Key);
                _detailsPrimarySkills.Add(new SWMSkillRowVM(ranked[i].Key, ranked[i].Value, true));
            }

            // Untrained skills are left out entirely, the way a vanilla wanderer sheet reads.
            for (int i = 0; i < ranked.Count; i++)
            {
                if (ranked[i].Value <= 0)
                    continue;
                _detailsAllSkills.Add(new SWMSkillRowVM(ranked[i].Key, ranked[i].Value, top.Contains(ranked[i].Key)));
            }

            DetailsPrimarySkills = _detailsPrimarySkills;
            DetailsAllSkills = _detailsAllSkills;
        }

        public void ExecuteViewCharacter()
        {
            if (_detailsOffer == null)
                return;
            CharacterViewModel model = WandererAppearance.BuildPreview(_detailsOffer);
            if (model == null)
            {
                MarketState.Ensure().StatusMessage = "Could not render that wanderer.";
                RefreshChrome();
                return;
            }
            DetailsCharacter = model;
            IsCharacterViewOpen = true;
        }

        public void ExecuteCloseCharacterView()
        {
            IsCharacterViewOpen = false;
            DetailsCharacter = null;
        }

        public void ExecuteCloseDetails()
        {
            // Side dossier stays open; escape/close exits the whole market.
            ClearSelection();
        }

        public void ExecuteClose()
        {
            SWMMarketScreen.Close();
        }

        public bool HandleEscape()
        {
            // Escape backs out of the character view before it closes the market.
            if (IsCharacterViewOpen)
            {
                ExecuteCloseCharacterView();
                return true;
            }
            SWMMarketScreen.Close();
            return true;
        }

        public static bool CanHire(WandererOffer offer, out string reason)
        {
            return SWMMarketApi.CanBuy(offer, out reason);
        }

        private static string TitleCulture(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static string TitleQuality(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "Common";
            if (s == "mid")
                return "Decent";
            if (s == "high")
                return "Skilled";
            if (s == "elite")
                return "Rare";
            if (s == "legendary")
                return "Exceptional";
            if (s == "auction")
                return "Auction Prize";
            if (s == "low")
                return "Green";
            return TitleCulture(s);
        }

        [DataSourceProperty]
        public MBBindingList<SWMWandererRowVM> Rows
        {
            get { return _rows; }
            set { if (_rows != value) { _rows = value; OnPropertyChangedWithValue(value, "Rows"); } }
        }

        [DataSourceProperty]
        public MBBindingList<SWMSkillRowVM> DetailsPrimarySkills
        {
            get { return _detailsPrimarySkills; }
            set { if (_detailsPrimarySkills != value) { _detailsPrimarySkills = value; OnPropertyChangedWithValue(value, "DetailsPrimarySkills"); } }
        }

        [DataSourceProperty]
        public MBBindingList<SWMSkillRowVM> DetailsAllSkills
        {
            get { return _detailsAllSkills; }
            set { if (_detailsAllSkills != value) { _detailsAllSkills = value; OnPropertyChangedWithValue(value, "DetailsAllSkills"); } }
        }

        [DataSourceProperty]
        public string TitleText
        {
            get { return _titleText; }
            set { if (_titleText != value) { _titleText = value; OnPropertyChangedWithValue(value, "TitleText"); } }
        }

        [DataSourceProperty]
        public string GoldText
        {
            get { return _goldText; }
            set { if (_goldText != value) { _goldText = value; OnPropertyChangedWithValue(value, "GoldText"); } }
        }

        [DataSourceProperty]
        public string TimerText
        {
            get { return _timerText; }
            set { if (_timerText != value) { _timerText = value; OnPropertyChangedWithValue(value, "TimerText"); } }
        }

        [DataSourceProperty]
        public string StatusText
        {
            get { return _statusText; }
            set { if (_statusText != value) { _statusText = value; OnPropertyChangedWithValue(value, "StatusText"); } }
        }

        [DataSourceProperty]
        public string CompanionCountText
        {
            get { return _companionCountText; }
            set { if (_companionCountText != value) { _companionCountText = value; OnPropertyChangedWithValue(value, "CompanionCountText"); } }
        }

        [DataSourceProperty]
        public string StockCountText
        {
            get { return _stockCountText; }
            set { if (_stockCountText != value) { _stockCountText = value; OnPropertyChangedWithValue(value, "StockCountText"); } }
        }

        [DataSourceProperty]
        public string SortHintText
        {
            get { return _sortHintText; }
            set { if (_sortHintText != value) { _sortHintText = value; OnPropertyChangedWithValue(value, "SortHintText"); } }
        }

        [DataSourceProperty]
        public bool IsDetailsOpen
        {
            get { return _isDetailsOpen; }
            set { if (_isDetailsOpen != value) { _isDetailsOpen = value; OnPropertyChangedWithValue(value, "IsDetailsOpen"); } }
        }

        [DataSourceProperty]
        public bool HasSelection
        {
            get { return _hasSelection; }
            set { if (_hasSelection != value) { _hasSelection = value; OnPropertyChangedWithValue(value, "HasSelection"); } }
        }

        [DataSourceProperty]
        public string DetailsName
        {
            get { return _detailsName; }
            set { if (_detailsName != value) { _detailsName = value; OnPropertyChangedWithValue(value, "DetailsName"); } }
        }

        [DataSourceProperty]
        public bool IsCharacterViewOpen
        {
            get { return _isCharacterViewOpen; }
            set { if (_isCharacterViewOpen != value) { _isCharacterViewOpen = value; OnPropertyChangedWithValue(value, "IsCharacterViewOpen"); } }
        }

        [DataSourceProperty]
        public CharacterViewModel DetailsCharacter
        {
            get { return _detailsCharacter; }
            set { if (_detailsCharacter != value) { _detailsCharacter = value; OnPropertyChangedWithValue(value, "DetailsCharacter"); } }
        }

        [DataSourceProperty]
        public CharacterViewModel AuctionCharacter
        {
            get { return _auctionCharacter; }
            set { if (_auctionCharacter != value) { _auctionCharacter = value; OnPropertyChangedWithValue(value, "AuctionCharacter"); } }
        }

        [DataSourceProperty]
        public string DetailsQuality
        {
            get { return _detailsQuality; }
            set { if (_detailsQuality != value) { _detailsQuality = value; OnPropertyChangedWithValue(value, "DetailsQuality"); } }
        }

        [DataSourceProperty]
        public string DetailsRole
        {
            get { return _detailsRole; }
            set { if (_detailsRole != value) { _detailsRole = value; OnPropertyChangedWithValue(value, "DetailsRole"); } }
        }

        [DataSourceProperty]
        public string DetailsCulture
        {
            get { return _detailsCulture; }
            set { if (_detailsCulture != value) { _detailsCulture = value; OnPropertyChangedWithValue(value, "DetailsCulture"); } }
        }

        [DataSourceProperty]
        public string DetailsAge
        {
            get { return _detailsAge; }
            set { if (_detailsAge != value) { _detailsAge = value; OnPropertyChangedWithValue(value, "DetailsAge"); } }
        }

        [DataSourceProperty]
        public string DetailsGender
        {
            get { return _detailsGender; }
            set { if (_detailsGender != value) { _detailsGender = value; OnPropertyChangedWithValue(value, "DetailsGender"); } }
        }

        [DataSourceProperty]
        public string DetailsPriceValue
        {
            get { return _detailsPriceValue; }
            set { if (_detailsPriceValue != value) { _detailsPriceValue = value; OnPropertyChangedWithValue(value, "DetailsPriceValue"); } }
        }

        [DataSourceProperty]
        public string DetailsCannotBuyReason
        {
            get { return _detailsCannotBuyReason; }
            set { if (_detailsCannotBuyReason != value) { _detailsCannotBuyReason = value; OnPropertyChangedWithValue(value, "DetailsCannotBuyReason"); } }
        }

        [DataSourceProperty]
        public bool DetailsCanBuy
        {
            get { return _detailsCanBuy; }
            set { if (_detailsCanBuy != value) { _detailsCanBuy = value; OnPropertyChangedWithValue(value, "DetailsCanBuy"); } }
        }

        [DataSourceProperty]
        public bool IsMarketTab
        {
            get { return _isMarketTab; }
            set { if (_isMarketTab != value) { _isMarketTab = value; OnPropertyChangedWithValue(value, "IsMarketTab"); } }
        }

        [DataSourceProperty]
        public bool IsAuctionTab
        {
            get { return _isAuctionTab; }
            set { if (_isAuctionTab != value) { _isAuctionTab = value; OnPropertyChangedWithValue(value, "IsAuctionTab"); } }
        }

        [DataSourceProperty]
        public MBBindingList<SWMSkillRowVM> AuctionPrimarySkills
        {
            get { return _auctionPrimarySkills; }
            set { if (_auctionPrimarySkills != value) { _auctionPrimarySkills = value; OnPropertyChangedWithValue(value, "AuctionPrimarySkills"); } }
        }

        [DataSourceProperty]
        public MBBindingList<SWMSkillRowVM> AuctionAllSkills
        {
            get { return _auctionAllSkills; }
            set { if (_auctionAllSkills != value) { _auctionAllSkills = value; OnPropertyChangedWithValue(value, "AuctionAllSkills"); } }
        }

        [DataSourceProperty]
        public MBBindingList<SWMLogLineVM> AuctionLog
        {
            get { return _auctionLog; }
            set { if (_auctionLog != value) { _auctionLog = value; OnPropertyChangedWithValue(value, "AuctionLog"); } }
        }

        [DataSourceProperty]
        public string AuctionName
        {
            get { return _auctionName; }
            set { if (_auctionName != value) { _auctionName = value; OnPropertyChangedWithValue(value, "AuctionName"); } }
        }

        [DataSourceProperty]
        public string AuctionRole
        {
            get { return _auctionRole; }
            set { if (_auctionRole != value) { _auctionRole = value; OnPropertyChangedWithValue(value, "AuctionRole"); } }
        }

        [DataSourceProperty]
        public string AuctionCulture
        {
            get { return _auctionCulture; }
            set { if (_auctionCulture != value) { _auctionCulture = value; OnPropertyChangedWithValue(value, "AuctionCulture"); } }
        }

        [DataSourceProperty]
        public string AuctionAge
        {
            get { return _auctionAge; }
            set { if (_auctionAge != value) { _auctionAge = value; OnPropertyChangedWithValue(value, "AuctionAge"); } }
        }

        [DataSourceProperty]
        public string AuctionGender
        {
            get { return _auctionGender; }
            set { if (_auctionGender != value) { _auctionGender = value; OnPropertyChangedWithValue(value, "AuctionGender"); } }
        }

        [DataSourceProperty]
        public string AuctionQuality
        {
            get { return _auctionQuality; }
            set { if (_auctionQuality != value) { _auctionQuality = value; OnPropertyChangedWithValue(value, "AuctionQuality"); } }
        }

        [DataSourceProperty]
        public string AuctionHighBidText
        {
            get { return _auctionHighBidText; }
            set { if (_auctionHighBidText != value) { _auctionHighBidText = value; OnPropertyChangedWithValue(value, "AuctionHighBidText"); } }
        }

        [DataSourceProperty]
        public string AuctionHighBidderText
        {
            get { return _auctionHighBidderText; }
            set { if (_auctionHighBidderText != value) { _auctionHighBidderText = value; OnPropertyChangedWithValue(value, "AuctionHighBidderText"); } }
        }

        [DataSourceProperty]
        public string AuctionTimerText
        {
            get { return _auctionTimerText; }
            set { if (_auctionTimerText != value) { _auctionTimerText = value; OnPropertyChangedWithValue(value, "AuctionTimerText"); } }
        }

        [DataSourceProperty]
        public string AuctionStatusText
        {
            get { return _auctionStatusText; }
            set { if (_auctionStatusText != value) { _auctionStatusText = value; OnPropertyChangedWithValue(value, "AuctionStatusText"); } }
        }

        [DataSourceProperty]
        public string AuctionPlayerBidText
        {
            get { return _auctionPlayerBidText; }
            set { if (_auctionPlayerBidText != value) { _auctionPlayerBidText = value; OnPropertyChangedWithValue(value, "AuctionPlayerBidText"); } }
        }

        [DataSourceProperty]
        public string AuctionCooldownText
        {
            get { return _auctionCooldownText; }
            set { if (_auctionCooldownText != value) { _auctionCooldownText = value; OnPropertyChangedWithValue(value, "AuctionCooldownText"); } }
        }

        [DataSourceProperty]
        public string AuctionMinNextBidText
        {
            get { return _auctionMinNextBidText; }
            set { if (_auctionMinNextBidText != value) { _auctionMinNextBidText = value; OnPropertyChangedWithValue(value, "AuctionMinNextBidText"); } }
        }

        [DataSourceProperty]
        public string BidInputText
        {
            get { return _bidInputText; }
            set { if (_bidInputText != value) { _bidInputText = value; OnPropertyChangedWithValue(value, "BidInputText"); } }
        }

        [DataSourceProperty]
        public string AuctionBidError
        {
            get { return _auctionBidError; }
            set { if (_auctionBidError != value) { _auctionBidError = value; OnPropertyChangedWithValue(value, "AuctionBidError"); } }
        }

        [DataSourceProperty]
        public bool AuctionHasLot
        {
            get { return _auctionHasLot; }
            set { if (_auctionHasLot != value) { _auctionHasLot = value; OnPropertyChangedWithValue(value, "AuctionHasLot"); } }
        }

        [DataSourceProperty]
        public bool AuctionCanBid
        {
            get { return _auctionCanBid; }
            set { if (_auctionCanBid != value) { _auctionCanBid = value; OnPropertyChangedWithValue(value, "AuctionCanBid"); } }
        }

        [DataSourceProperty]
        public bool AuctionClosed
        {
            get { return _auctionClosed; }
            set { if (_auctionClosed != value) { _auctionClosed = value; OnPropertyChangedWithValue(value, "AuctionClosed"); } }
        }
    }
}
