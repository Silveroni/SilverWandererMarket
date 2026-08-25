#!/usr/bin/env bash
set -euo pipefail
export PATH="$HOME/.dotnet:$PATH"
CSC="${CSC:-$HOME/.dotnet/sdk/8.0.423/Roslyn/bincore/csc.dll}"
GAME="/storage/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord"
DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="$DIR/../bin/Win64_Shipping_Client"
BIN="$GAME/bin/Win64_Shipping_Client"
MONO="$BIN/mono/lib/mono/4.7.2-api"
mkdir -p "$OUT"
dotnet exec "$CSC" -nologo -t:library -langversion:10 -nostdlib \
  -r:"$MONO/mscorlib.dll" \
  -r:"$MONO/System.dll" \
  -r:"$MONO/System.Core.dll" \
  -r:"$MONO/Facades/netstandard.dll" \
  -r:"$BIN/TaleWorlds.DotNet.dll" \
  -r:"$BIN/TaleWorlds.Library.dll" \
  -r:"$BIN/TaleWorlds.Localization.dll" \
  -r:"$BIN/TaleWorlds.Core.dll" \
  -r:"$BIN/TaleWorlds.Core.ViewModelCollection.dll" \
  -r:"$BIN/TaleWorlds.Engine.dll" \
  -r:"$BIN/TaleWorlds.Engine.GauntletUI.dll" \
  -r:"$BIN/TaleWorlds.GauntletUI.dll" \
  -r:"$BIN/TaleWorlds.InputSystem.dll" \
  -r:"$BIN/TaleWorlds.ScreenSystem.dll" \
  -r:"$BIN/TaleWorlds.MountAndBlade.dll" \
  -r:"$BIN/TaleWorlds.CampaignSystem.dll" \
  -r:"$BIN/TaleWorlds.ObjectSystem.dll" \
  -out:"$OUT/SilverWandererMarket.dll" \
  "$DIR/SWMSubModule.cs" \
  "$DIR/SWMLog.cs" \
  "$DIR/Behaviors/WandererMarketCampaignBehavior.cs" \
  "$DIR/Market/MarketConfig.cs" \
  "$DIR/Market/WandererOffer.cs" \
  "$DIR/Market/Archetypes.cs" \
  "$DIR/Market/RoleDraft.cs" \
  "$DIR/Market/QualityCurve.cs" \
  "$DIR/Market/PriceCalculator.cs" \
  "$DIR/Market/OfferGenerator.cs" \
  "$DIR/Market/AuctionState.cs" \
  "$DIR/Market/AuctionAi.cs" \
  "$DIR/Market/MarketState.cs" \
  "$DIR/Market/SWMAuctionApi.cs" \
  "$DIR/Market/SWMAuctionEscrow.cs" \
  "$DIR/Market/SWMAuctionHooks.cs" \
  "$DIR/Market/SWMMarketApi.cs" \
  "$DIR/Market/SWMMarketHooks.cs" \
  "$DIR/Market/SessionProbe.cs" \
  "$DIR/Spawn/TavernBrokerSpawner.cs" \
  "$DIR/Spawn/BrokerHeroService.cs" \
  "$DIR/Spawn/SWMHeroAgentLocationModel.cs" \
  "$DIR/Dialog/BrokerDialog.cs" \
  "$DIR/Dialog/HiredWandererDialog.cs" \
  "$DIR/Heroes/HiredWanderer.cs" \
  "$DIR/Heroes/WandererAppearance.cs" \
  "$DIR/Heroes/CompanionFactory.cs" \
  "$DIR/UI/SWMMarketVM.cs" \
  "$DIR/UI/SWMMarketScreen.cs"
echo "built $OUT/SilverWandererMarket.dll"
