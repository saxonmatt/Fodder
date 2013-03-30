[<AutoOpen>]
module GameAlgorithms

    [<AutoOpen>]
    module Dudes = 

        let UpdateDude activeDude = 
            let getDude (Active(x:Dude)) = x
            let dude = activeDude |> getDude
            { dude with Team = BlueTeam } |> ignore
            ()

        let test dude = 
            let activeDude = Active(dude)            
            UpdateDude activeDude