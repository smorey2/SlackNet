using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlackNet.Blocks;
using SlackNet.Events;
using SlackNet.Interaction;
using SlackNet.WebApi;
using Button = SlackNet.Blocks.Button;

namespace SlackNet.EventsExample
{
    public class AppHome : IEventHandler<AppHomeOpened>, IBlockActionHandler<ButtonAction>, IViewSubmissionHandler
    {
        private const string OpenModal = "open_modal";
        private const string InputBlockId = "input_block";
        private const string InputActionId = "text_input";
        private const string SingleSelectActionId = "single_select";
        private const string MultiSelectActionId = "multi_select";
        private const string DatePickerActionId = "date_picker";
        private const string TimePickerActionId = "time_picker";
        private const string RadioActionId = "radio";
        private const string CheckboxActionId = "checkbox";
        private const string SingleUserActionId = "single_user";
        public static readonly string ModalCallbackId = "home_modal";

        private readonly ISlackApiClient _slack;
        public AppHome(ISlackApiClient slack) => _slack = slack;

        public async Task Handle(AppHomeOpened slackEvent)
        {
            if (slackEvent.Tab == AppHomeTab.Home)
                await _slack.Views.Publish(slackEvent.User, new HomeViewDefinition
                    {
                        Blocks =
                            {
                                new SectionBlock { Text = "Welcome to the SlackNet example home" },
                                new ActionsBlock
                                    {
                                        Elements =
                                            {
                                                new Button
                                                    {
                                                        Text = "Open modal",
                                                        ActionId = OpenModal
                                                    }
                                            }
                                    }
                            }
                    }, slackEvent.View?.Hash).ConfigureAwait(false);
        }

        public async Task Handle(ButtonAction action, BlockActionRequest request)
        {
            if (action.ActionId == OpenModal)
                await _slack.Views.Open(request.TriggerId, new ModalViewDefinition
                    {
                        Title = "Example Modal",
                        CallbackId = ModalCallbackId,
                        Blocks =
                            {
                                new InputBlock
                                    {
                                        Label = "Input",
                                        BlockId = InputBlockId,
                                        Element = new PlainTextInput
                                            {
                                                ActionId = InputActionId,
                                                Placeholder = "Enter some text"
                                            }
                                    },
                                new InputBlock
                                    {
                                        Label = "Single-select",
                                        BlockId = "single_select_block",
                                        Element = new StaticSelectMenu
                                            {
                                                ActionId = SingleSelectActionId,
                                                Options = ExampleOptions()
                                            }
                                    },
                                new InputBlock
                                    {
                                        Label = "Multi-select",
                                        BlockId = "multi_select_block",
                                        Element = new StaticMultiSelectMenu
                                            {
                                                ActionId = MultiSelectActionId,
                                                Options = ExampleOptions()
                                            }
                                    },
                                new InputBlock
                                    {
                                        Label = "Date",
                                        BlockId = "date_block",
                                        Element = new DatePicker { ActionId = DatePickerActionId }
                                    },
                                new InputBlock
                                    {
                                        Label = "Time",
                                        BlockId = "time_block",
                                        Element = new TimePicker { ActionId = TimePickerActionId }
                                    },
                                new InputBlock
                                    {
                                        Label = "Radio options",
                                        BlockId = "radio_block",
                                        Element = new RadioButtonGroup
                                            {
                                                ActionId = RadioActionId,
                                                Options = ExampleOptions()
                                            }
                                    },
                                new InputBlock
                                    {
                                        Label = "Checkbox options",
                                        BlockId = "checkbox_block",
                                        Element = new CheckboxGroup
                                            {
                                                ActionId = CheckboxActionId,
                                                Options = ExampleOptions()
                                            }
                                    },
                                new InputBlock
                                    {
                                        Label = "Single user select",
                                        BlockId = "single_user_block",
                                        Element = new UserSelectMenu
                                            {
                                                ActionId = SingleUserActionId
                                            }
                                    }
                            },
                        Submit = "Submit",
                        NotifyOnClose = true
                    }).ConfigureAwait(false);
        }

        private IList<Blocks.Option> ExampleOptions()
        {
            return new List<Blocks.Option>
                {
                    new Blocks.Option { Text = "One", Value = "1" },
                    new Blocks.Option { Text = "Two", Value = "2" },
                    new Blocks.Option { Text = "Three", Value = "3" }
                };
        }

        public async Task<ViewSubmissionResponse> Handle(ViewSubmission viewSubmission)
        {
            var state = viewSubmission.View.State;
            var values = new Dictionary<string, string>
                {

                    { "Input", state.GetValue<PlainTextInputValue>(InputActionId).Value },
                    { "Single-select", state.GetValue<StaticSelectValue>(SingleSelectActionId).SelectedOption?.Text.Text ?? "none" },
                    { "Multi-select", string.Join(", ", state.GetValue<StaticMultiSelectValue>(MultiSelectActionId).SelectedOptions.Select(o => o.Text).DefaultIfEmpty("none")) },
                    { "Date", state.GetValue<DatePickerValue>(DatePickerActionId).SelectedDate?.ToString("yyyy-MM-dd") ?? "none" },
                    { "Time", state.GetValue<TimePickerValue>(TimePickerActionId).SelectedTime?.ToString("hh\\:mm") ?? "none" },
                    { "Radio options", state.GetValue<RadioButtonGroupValue>(RadioActionId).SelectedOption?.Text.Text ?? "none" },
                    { "Checkbox options", string.Join(", ", state.GetValue<CheckboxGroupValue>(CheckboxActionId).SelectedOptions.Select(o => o.Text).DefaultIfEmpty("none")) },
                    { "Single user select", state.GetValue<UserSelectValue>(SingleUserActionId).SelectedUser ?? "none" }
                };

            await _slack.Chat.PostMessage(new Message
                {
                    Channel = await UserIm(viewSubmission.User).ConfigureAwait(false),
                    Text = $"You entered: {state.GetValue<PlainTextInputValue>(InputActionId).Value}",
                    Blocks =
                        {
                            new SectionBlock
                                {
                                    Text = new Markdown("You entered:\n"
                                        + string.Join("\n", values.Select(kv => $"*{kv.Key}:* {kv.Value}")))
                                }
                        }
                }).ConfigureAwait(false);

            return ViewSubmissionResponse.Null;
        }

        public async Task HandleClose(ViewClosed viewClosed)
        {
            await _slack.Chat.PostMessage(new Message
                {
                    Channel = await UserIm(viewClosed.User).ConfigureAwait(false),
                    Text = "You cancelled the modal"
                }).ConfigureAwait(false);
        }

        private Task<string> UserIm(User user)
        {
            return _slack.Conversations.Open(new[] { user.Id });
        }
    }
}