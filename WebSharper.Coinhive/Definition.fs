namespace WebSharper.Coinhive

open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =
    let Theme =
        Pattern.EnumInlines "Theme" <|
        [
            "Light", "light"
            "Dark", "dark"
        ]
        |> WithComment "The color theme for the opt-in screen - AuthedMine only."

    let OptInStatus =
        Pattern.EnumInlines "OptInStatus" <|
        [
            "Accepted", "accepted"
            "Canceled", "canceled"
        ]
        |> WithComment "Whether the user accepted the opt0in to the mining, or not."

    let Options =
        Pattern.Config "options" {
            Required = []
            Optional =
            [
                "threads", T<byte>
                "throttle", T<float>
                "forceASMJS", T<bool>
                "theme", Theme.Type
                "language", T<string>
            ]
        }

    let StartMode =
        Pattern.EnumInlines "mode" [
            "IfExclusiveTab", "CoinHive.IF_EXCLUSIVE_TAB"
            "ForceExclusiveTab", "CoinHive.FORCE_EXCLUSIVE_TAB	"
            "ForceMultiTab", "CoinHive.FORCE_MULTI_TAB	"
        ]

    let Coinhive =
        Class "CoinHive"
        |+> Static [
            "Anonymous" => T<string>?siteKey ^-> !?Options ^-> TSelf
            |> WithComment """
Create a new miner that is not attached to a token or user name.
Common use-cases include donations to your website, where users just run the miner without any direct incentives for solved hashes."""

            "User" => T<string>?siteKey ^-> T<string> ^-> !?Options ^-> TSelf
            |> WithComment """Create a new miner and credit all hashes to the specified user name.
You can check a user's balance and withdraw hashes for a user with our [HTTP API](https://coinhive.com/documentation/http-api#user-balance).
Common use-cases include granting in-game currency or other incentives to a user account on your website in turn for running the miner.
Please only use the `CoinHive.User` miner if you later intend to retreive the number of hashes using the HTTP API.
Don't use it to store random session names that you never read back."""

            "Token" => T<string>?siteKey ^-> T<uint32>?targetHashes ^-> !?Options?options ^-> TSelf
            |> WithComment """Create a new miner and stop once the specified number of hashes (`targetHashes`) was found.
Tokens can be verified with our [HTTP API](https://coinhive.com/documentation/http-api#user-balance).
Tokens remain valid for 1 hour after they have reached the target.\nThe random token name is created by our mining pool.
You can read it client side with `getToken()` after the miner successfully authed on the pool.
Common use-cases include one off proof of work verifications to limit actions on your site or grant access to a resource.
For example, this is used by the Coinhive captcha and shortlinks."""
        ]
        |+> Instance [
            "Start" => !?StartMode?startMode ^-> T<unit>
            |> WithComment """Connect to the pool and start mining.
The optional mode parameter specifies how the miner should behave if a miner in another tab is already running.
The default is `CoinHive.IF_EXCLUSIVE_TAB`."""
            |> WithSourceName "start"

            "Stop" => T<unit> ^-> T<unit>
            |> WithComment "Stop mining and disconnect from the pool."
            |> WithSourceName "stop"

            "IsRunning" => T<unit> ^-> T<bool>
            |> WithComment "Returns whether the miner is currently running: connected to the pool and has working threads."
            |> WithSourceName "isRunning"

            "IsMobile" => T<unit> ^-> T<bool>
            |> WithComment "Returns whether the user is using a phone or tablet device. You can use this to only start the miner on laptops and PCs."
            |> WithSourceName "isMobile"

            "DidOptOut" => !?T<int>?seconds ^-> T<bool>
            |> WithComment """Returns whether the user has clicked the "Cancel" button in the opt-in screen in the last `seconds` seconds.
The seconds parameter is optional and defaults to 14400 (4 hours).
You can use this function to only show the opt-in screen again after a certain time, if the user has canceled the previous opt-in."""

        ]

    let Assembly =
        Assembly [
            Namespace "WebSharper.Coinhive" [
                Theme
                Options
                StartMode
                Coinhive
            ]
            Namespace "WebSharper.Coinhive.Resources" [
                Resource "Coinhive" "https://coinhive.com/lib/coinhive.min.js" |> WithComment """The Coinhive library.
__WARNING__: This library does _not_ inform users that their CPU resources are used to mine Monero,
and it might be blocked by ad-blocking, or worse, antivirus software.
Use `AuthedMine` instead."""

                Resource "AuthedMine" "https://authedmine.com/lib/authedmine.min.js" |> WithComment """A version of Coinhive's miner that is not blocked by adblockers but requires an explicit opt-in from the end-user.
[See more here](https://coinhive.com/documentation/authedmine)."""
            ]
        ]

[<Sealed>]
type CoinhiveExtension() =
    interface IExtension with
        member __.Assembly = Definition.Assembly

[<assembly: Extension(typeof<CoinhiveExtension>)>]
do()