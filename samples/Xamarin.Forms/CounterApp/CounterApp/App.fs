namespace Fabulous.XamarinForms.Samples.CounterApp

open Xamarin.Forms
open Fabulous
open Fabulous.XamarinForms
open type Fabulous.XamarinForms.View

module Style =
    let createStyleFor<'T when 'T :> BindableObject> setters =
        let style = Style(typeof<'T>)
        for property, value in setters do
            style.Setters.Add(Setter(Property = property, Value = value))
        style

    let resources =
        let resources = ResourceDictionary()
        resources.Add(
            // make sure you define the Element type for the Style so the global style can be applied
            createStyleFor<Label>
                [ Label.TextColorProperty, box Color.Green
                  Label.FontSizeProperty, box 24. ])
        resources

module Configuration =
    type Model =
        { Step: int
          TimerOn: bool }
    
    type Msg =
        | StepChanged of double
        | TimerToggled of bool
        
    let init () =
        { Step = 1
          TimerOn = false }
        
    let update msg model =
        match msg with
        | StepChanged n -> { model with Step = int (n + 0.5) }
        | TimerToggled on -> { model with TimerOn = on }
        
    let timerView timerOn =
        HorizontalStackLayout() {
            Label($"Timer - {System.DateTime.Now}")

            Switch(timerOn, TimerToggled)
                .automationId("TimerSwitch")
        }
        
    let view model =
        VerticalStackLayout() {
            View.memo model.TimerOn timerView
            
            Slider(min = 0., max = 10., value = float model.Step, onValueChanged = StepChanged)
                .automationId("StepSlider")
                .centerHorizontal()

            Label($"Step size: {model.Step}")
                .automationId("StepSizeLabel")
                .centerHorizontal()
        }

module App =
    type Model =
        { Count: int
          Data: int list
          Configuration: Configuration.Model }

    type Msg =
        | ConfigurationMsg of Configuration.Msg
        | Increment
        | Decrement
        | Reset
        | TimedTick
        | OsThemeChanged of OSAppTheme
        | ItemsThresholdReached

    type CmdMsg =
        | TickTimer

    let timerCmd () =
        async {
            do! Async.Sleep 200
            return TimedTick
        }
        |> Cmd.ofAsyncMsg

    let mapCmdMsgToCmd cmdMsg =
        match cmdMsg with
        | TickTimer -> timerCmd ()

    let initModel () =
        { Count = 0
          Data = [ for i in 0 .. 100 do i ]
          Configuration = Configuration.init () }
        
    let init () =
        initModel (), []

    let update msg model =
        match msg with
        | ConfigurationMsg cMsg ->
            let newConf = Configuration.update cMsg model.Configuration            
            { model with Configuration = newConf }, [ if newConf.TimerOn = true && model.Configuration.TimerOn = false then TickTimer ]
            
        | Increment -> { model with Count = model.Count + model.Configuration.Step }, []
        | Decrement -> { model with Count = model.Count - model.Configuration.Step }, []
        | Reset -> init ()
        | TimedTick ->
            if model.Configuration.TimerOn then { model with Count = model.Count + model.Configuration.Step }, [ TickTimer ] else model, []
        | OsThemeChanged osAppTheme ->
            printfn $"{osAppTheme}"
            model, []
        | ItemsThresholdReached ->
            { model with Data = List.append model.Data [ for i = model.Data.Length to model.Data.Length + 100 do i ] }, []
            
    type OtherMsg = Test
    
    let view model =
        Application(
            NavigationPage() {
                ContentPage("Counter",
                    VerticalStackLayout() {
                        ListView(model.Data) (fun item ->
                            TextCell(item.ToString())
                                .textColor(Color.Blue)
                        )
                        
//                        (GroupedListView(model.Data, fun group ->
//                            TextCell(group.Name)
//                        ) (fun item ->
//                            TextCell(item.Name)
//                        ))
//                            .showJumpList(true, fun group -> group.ShortName)
                    }
                ).hasNavigationBar(false)
            }
        )
         .onRequestedThemeChanged(OsThemeChanged)
         .resources(Style.resources)

    let program = Program.statefulApplicationWithCmdMsg init update view mapCmdMsgToCmd
