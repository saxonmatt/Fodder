[<AutoOpen>]
module RuleTypes
    
    type Active<'a> = Active of 'a
    type Inactive<'a> = Inactive of 'a