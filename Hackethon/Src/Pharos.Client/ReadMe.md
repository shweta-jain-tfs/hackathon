### Components / Views / Pages
*Use code behind:* Do not use @code in the MyComponent.razor file. Add a new MyComponent.razor.cs file instead.

- Projects:
`Pharos.Client`
`Pharos.Common.Web`

 - Naming components (do not use the word component in the name it is just here for the example): 
`MyComponent.razor`
`MyComponent.razor.cs`

- A View is a component that has a ViewModel. Here you do add the suffix View.
`MyView.razor`
`MyView.razor`

- Example of Pharos Razor file: 
  [Pharos Razor file example](https://idd-devops-p1.emea.thermo.com/DevelopmentCollection/TetrisSoftware/_wiki/wikis/Pharos.wiki/50/Razor-file-example-(with-specific-order))
- What does Microsoft write:
  [Microsoft Blazor Component Guidelines](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/?view=aspnetcore-8.0)

### Public Events
- Event Naming:
```
[Parameter] public EventCallback<bool> OnButtonClick { get; set; }
[Parameter] public EventCallback<bool> OnRowSelected { get; set; }
[Parameter] public EventCallback<bool> OnItemChanged { get; set; }
```
- 
### Event handlers
```
protected Task ButtonClicked() { }

protected Task RowSelected() { }

protected Task ItemChanged() { }
```
 
####Ex
EventCallback<int> OnItemSelected()

- Event handler naming

`() => SelectItem(Item.Id)`
`x => SelectItem(x)`
`ItemSelected`

#### Example 
	
	<PhButton OnClick="@(x => SelectItem(x))">
	private void SelectItem(int id) {}

	<PhButton OnClick="@ItemSelected">
	private void ItemSelected(ChangeEventArgs e) {}

### Component Styling
- Project:
`Pharos.Common.Web`

- Naming: 
>`_ph-my-component.scss`
>>`.ph-my-component`
>>>`&__button`
>>>`&__result`
>>>`&--big`
>>>`&--disabled`

- [Writing SCSS components](https://idd-devops-p1.emea.thermo.com/DevelopmentCollection/_git/TetrisSoftware?path=/Src/Pharos.Common.Web/Styles/Components.md)


### Notifications (SignalR)
- Projects:
`Pharos.Client.Common.Models/Notifications`
`Pharos.Server.Infrastructure.Notifications`

- Example of a message (to search in code for):
`ReagentDrawerLockStateChanged`

### Testing
- Project:
`Pharos.Common.Web.Tests`

- Naming of unit tests:
`MyNameTests`

- Current Frameworks:
  |Test framework|Link|
  |--------------|------|
  |`bunit`| [a testing library for Blazor components](https://bunit.dev/) |
  |`Moq`  | [mocking library for .NET](https://www.nuget.org/packages/Moq) |
  |`unit` | [unit testing tool for the .NET Framework](https://xunit.net/) |




## CodeBehind example (with specific order)
```
using Microsoft.AspNetCore.Components;

namespace YourNamespace
{
    public partial class YourComponent : ComponentBase, IDisposable
    {
        // Injections
        [Inject]
        public SomeService Service1 { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        // Cascading parameter
        [CascadingParameter(Name = "MyCascadingParameter")]
        public MyCascadingValueType MyCascadingParameter { get; set; }

        // Parameters
        [Parameter]
        public string Parameter1 { get; set; }

        [Parameter]
        public int Parameter2 { get; set; }

        
        // Parameter Events
        [Parameter] //Click on the whole component
        public EventCallback<string> OnClick;

        [Parameter] //Click on a part inside a component
        public EventCallback<TGridItem> SelectedItemChanged;

        // Fields
        private int _count;

        // Properties
        public string SomeProperty { get; set; }


        // Lifecycle methods
        protected override async Task OnInitializedAsync()
        {
            // Initialization logic
            await base.OnInitializedAsync();
        }

        // Event handlers
        private void ButtonClickHandler()
        {
            // Event handling logic
            ButtonClicked?.Invoke("Button clicked!");
        }

        // Other methods
        private void IncrementCount()
        {
            _count++;
        }
    }
}
```