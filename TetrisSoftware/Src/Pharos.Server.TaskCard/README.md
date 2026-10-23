# Create and remove a Task Card (Code)

In Pharos, it is up to each module to create task cards for its own instances. The task cards are created as commands (CreateTaskCardCommand) and has a various of different constructors to suit different types of task cards and situation.

## CreateTaskCardCommand
The CreateTaskCardCommand can have the following parameters: 
  * CardType : The task card type is an enum that reflects what type of task card it is. Example: SampleDrawerOpen or Error
  * CardState : The task card state is an enum reflecting the state displayed on the task card. Example: Hidden, Information, Warning, Error, Flashing red
  * ExpiresAt : The expiresAt parameter is a TimeSpan that is possible to set and reflects the time left that displays and is ticking down in the task card
  * InstrumentNumber : The instrument number is a string and reflects the instrument serial number. It can be set to link the task card to a specific instrument.
  * Rules : The rules parameter is a List of EscalationRule. The rules reflect the "escalation" or "schedule" of the task card and makes it possible to auto escalate the card state according to a schedule

  ### EscalationRule
  The EscalationRule is a record that takes a CardState and a TimeSpan Trigger as parameters. By setting a list of escalation rules, you can set a schedule for the state of the task card. 
  Example: If you create a SampleDrawerOpen task card when you get from the instrument that the sample drawer is open. You don't want it to show as an info until after 5 minutes. Then after another 5 minutes you want to upgrade it to a warning and another 5 minuters to upgrade it to an error. Here you can use the parameter Rules when creating the task card command. Create a list<EscalationRule> and add the following Escalation rules:
  ```
      new EscalationRule(TaskCardState.Hidden, TimeSpan.Zero), //Starts as hidden
      new EscalationRule(TaskCardState.Information, TimeSpan.FromMinutes(5)), //Displays as information after 5 min
      new EscalationRule(TaskCardState.Warning, TimeSpan.FromMinutes(10)), //Upgrades to warning after another 5 min
      new EscalationRule(TaskCardState.Error, TimeSpan.FromMinutes(15)) //Upgrades to error after another 5 min
  ```
  The TaskCardService has a method that is run in an interval that goes though all active task cards and escalates them according to each task cards escalation rules

  ### Create Example
  Example of creating a sample drawer open task card:
  ```  
  await _mediator.Send(new TaskCardCommand(TaskCardType.SampleDrawerOpen,
                    [
                        new EscalationRule(TaskCardState.Hidden, TimeSpan.Zero),
                        new EscalationRule(TaskCardState.Information, TimeSpan.FromMinutes(5)),
                        new EscalationRule(TaskCardState.Warning, TimeSpan.FromMinutes(10)),
                        new EscalationRule(TaskCardState.Error, TimeSpan.FromMinutes(15))
                    ], TimeSpan.FromMinutes(7), instrumentNumber), cancellationToken);
```

## RemoveTaskCardCommand
To remove a task card, create a RemoveTaskCardCommand. This command takes CardType and InstrumentNumber as parameters. Since there can only be one task card active of each type and instrument, these two parameters are the only ones you need to find the task card to remove from the active task cards.

  ### Remove Example
  Example of removing a sample drawer open task card:
  ```
  await _mediator.Send(new RemoveTaskCardCommand(TaskCardType.SampleDrawerOpen, instrumentNumber), cancellationToken);
  ```

# Add new task card (Text and Image)
When adding a new task card to Pharos, a task card type, a title, an image and a connected page needs to be added in order for the task card to be able to be created properly. You also need to add your new taskcardtype to the suiting TaskCardRules

  ## TaskCardType
  The task card type is an enum. It sets for example the color of the TaskCard or the icon (if there is supposed to be a preset icon for that specific TaskCardType). When adding a new type of task card, you need to add that type to the TaskCardType enom in TaskCardCommand.cs. After adding the type, run nswag to update the generated model.

  ## Title
  The title is added to the translation.resx file. The format of the data name is TaskCard_{TaskCardType}_Title.

  ## Image
  The image is added as an inline SVG Razor component in `Src\Pharos.Common.Web\Components\TaskCard\Svg\`. The name of the component should follow the format `TaskCard_{TaskCardType}.razor`. After adding the Razor file, register the new `TaskCardImageType` enum value in `TaskCardImageType.cs` and add a dictionary entry in `PhTaskCardImage.razor.cs`.

  ## Connected Page
  The connected page is the landing page that is routed to when clicking on the task card. When adding a new task card, you need to add the route to the switch case in TaskCardConnectedPage.cs. The system will fetch the route from here depending on the task card type

  ## TaskCardRules
  The task card rules are rules that are in place to easily know what task cards types are affected by different situations. When adding a new task card type, you need to add the type to the correct task card rules. Example: There is a task card rule named "instrumentmandatory", this rule should contain all task cards that are connected to an instrument (ex SampleDrawerOpen).