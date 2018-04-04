namespace WebSharper.Coinhive

open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =
    let Theme = Pattern.EnumStrings "Theme" ["light"; "dark"]

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
            "IfExclusiveTab", "ifExclusiveTab"
            "ForceExclusiveTab", "forceExclusiveTab"
            "ForceMultiTab", "forceMultiTab"
        ]

    let Coinhive =
        Class "CoinHive"
        |+> Static [
            "Anonymous" => T<string> ^-> !?Options ^-> TSelf
            |> WithComment "Create a new miner that is not attached to a token or user name.\nCommon use-cases include donations to your website, where users just run the miner without any direct incentives for solved hashes."

            "User" => T<string> ^-> T<string> ^-> !?Options ^-> TSelf
            |> WithComment "Create a new miner and credit all hashes to the specified user name. You can check a user's balance and withdraw hashes for a user with our [HTTP API](https://coinhive.com/documentation/http-api#user-balance).\nCommon use-cases include granting in-game currency or other incentives to a user account on your website in turn for running the miner.\nPlease only use the `CoinHive.User` miner if you later intend to retreive the number of hashes using the HTTP API. Don't use it to store random session names that you never read back."

            "Token" => T<string> ^-> T<uint32> ^-> !?Options ^-> TSelf
            |> WithComment "Create a new miner and stop once the specified number of hashes (`targetHashes`) was found. Tokens can be verified with our [HTTP API](https://coinhive.com/documentation/http-api#user-balance). Tokens remain valid for 1 hour after they have reached the target.\nThe random token name is created by our mining pool. You can read it client side with `getToken()` after the miner successfully authed on the pool.\nCommon use-cases include one off proof of work verifications to limit actions on your site or grant access to a resource. For example, this is used by the Coinhive captcha and shortlinks."
        ]
        |+> Instance [
            "start" => !?StartMode ^-> T<unit>
            |> WithComment "Connect to the pool and start mining. The optional mode parameter specifies how the miner should behave if a miner in another tab is already running. The default is `CoinHive.IF_EXCLUSIVE_TAB`."
            
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
                Resource "Coinhive" "https://coinhive.com/lib/coinhive.min.js" |> WithComment "The Coinhive library.\n__WARNING__: This library does _not_ inform users that their CPU resources are used to mine Monero, and it might be blocked by ad-blocking, or worse, antivirus software.\nUse `AuthedMine` instead."

                Resource "AuthedMine" "https://authedmine.com/lib/authedmine.min.js" |> WithComment "A version of Coinhive's miner that is not blocked by adblockers but requires an explicit opt-in from the end-user.\n[See more here](https://coinhive.com/documentation/authedmine)."
            ]
        ]
    
[<Sealed>]
type CoinhiveExtension() =
    interface IExtension with
        member __.Assembly = Definition.Assembly

[<assembly: Extension(typeof<CoinhiveExtension>)>]
do()