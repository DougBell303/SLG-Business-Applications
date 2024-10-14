﻿using System;
using TimHanewich.Dataverse;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using Spectre.Console;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

namespace CopilotStudioAnalytics
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DoThis().Wait();
        }

        public static async Task DoThis()
        {

            Console.WriteLine();
            AnsiConsole.MarkupLine("Welcome to :robot: [bold][blue]Copilot Studio Advanced Analytics[/][/] :robot:, a demonstration application by the [underline]Microsoft State & Local Government Business Applications team[/]!");
            Console.WriteLine();

            //Parse Dataverse Credentials from JSON in file.
            string dataverse_authenticator_json_path = @".\DataverseAuthenticator.json";
            string dataverse_authenticator_json = System.IO.File.ReadAllText(dataverse_authenticator_json_path);
            DataverseAuthenticator? auth = JsonConvert.DeserializeObject<DataverseAuthenticator>(dataverse_authenticator_json);
            if (auth == null)
            {
                throw new Exception("Unable to parse DataverseAuthenticator credentials from file '" + dataverse_authenticator_json_path + "'");
            }
            auth.ClientId = Guid.Parse("51f81489-12ee-4a9e-aaae-a2591f45987d"); //fill in default (standard) clientid
            AnsiConsole.MarkupLine("[italic]Dataverse credentials received![/]");
            
            //Authenticate (call to Dataverse OAuth API and get access token that we can then use to make)
            AnsiConsole.Markup("[green][bold]Dataverse Auth[/][/]: Authenticating as '" + auth.Username + "' to environment '" + auth.Resource + "'... ");
            await auth.GetAccessTokenAsync();      
            AnsiConsole.MarkupLine("[italic][green]Authenticated![/][/]");     

            //Retrieve transcripts
            DataverseService ds = new DataverseService(auth.Resource, auth.AccessToken);
            AnsiConsole.Markup("[green][bold]Dataverse Read[/][/]: Retrieving Copilot Studio transcripts... ");
            JArray transcripts = await ds.ReadAsync("conversationtranscripts");
            //JArray transcripts = JArray.Parse(System.IO.File.ReadAllText(@"C:\Users\timh\Downloads\SLG-Business-Applications\demos\Copilot-Studio-Advanced-Analytics\examples\conversationtranscripts.json")); //Collect from local, optionally
            AnsiConsole.MarkupLine("[italic][green]" + transcripts.Count.ToString("#,##0") + " transcripts retrieved![/][/]");

            //Retrieve bots
            AnsiConsole.Markup("[green][bold]Dataverse Read[/][/]: Retrieving list of Copilot Studio bots... ");
            JArray bots = await ds.ReadAsync("bots");
            AnsiConsole.MarkupLine("[italic][green]" + bots.Count.ToString("#,##0") + " bots found![/][/]");

            //Parse them!
            CopilotStudioBot[] csbots = CopilotStudioBot.Parse(bots, transcripts);

            //Retrieve users
            AnsiConsole.Markup("[green][bold]Dataverse Read[/][/]: Retrieving System Users... ");
            TimHanewich.Dataverse.AdvancedRead.DataverseReadOperation dro = new TimHanewich.Dataverse.AdvancedRead.DataverseReadOperation();
            dro.TableIdentifier = "systemusers";
            dro.AddColumn("fullname");
            dro.AddColumn("systemuserid");
            dro.AddColumn("internalemailaddress");
            JArray systemusers = await ds.ReadAsync(dro);
            SystemUser[] users = SystemUser.Parse(systemusers);
            AnsiConsole.MarkupLine("[italic][green]" + users.Length.ToString("#,##0") + " users found![/][/]");

            Console.WriteLine();
            
            # region "What to do next?"


            while (true)
            {
                
                //Prompt with options
                SelectionPrompt<string> DoNext = new SelectionPrompt<string>();
                DoNext.Title("What do you want to do next?");
                DoNext.AddChoices("Review environment-level statistics");
                DoNext.AddChoice("See bot ownership breakdown");
                DoNext.AddChoice("Examine an individual bot's usage");
                DoNext.AddChoice("Exit");
                string DoNextSelection = AnsiConsole.Prompt(DoNext);

                //Clear the console after they select an option
                Console.Clear();


                //Handle decisions
                if (DoNextSelection == "Review environment-level statistics")
                {
                    AnsiConsole.MarkupLine("[bold][underline][navy]Environment-Level Analytics[/][/][/]");
                    AnsiConsole.MarkupLine("[gray]Copilot Studio Statistics for enviornment '" + auth.Resource + "':[/]");

                    Table t = new Table();

                    //Add Statistic column
                    TableColumn tc_statistic = new TableColumn("Statistic");
                    tc_statistic.Alignment = Justify.Left;
                    t.AddColumn(tc_statistic);

                    //Add Value column
                    TableColumn tc_value = new TableColumn("Value");
                    tc_value.Alignment = Justify.Right;
                    t.AddColumn(tc_value);

                    //Add bots
                    t.AddRow("# of Bots", csbots.Length.ToString());


                    //Add sessions
                    int sessionsc = 0;
                    foreach (CopilotStudioBot bot in csbots)
                    {
                        sessionsc = sessionsc + bot.Sessions.Length;
                    }
                    t.AddRow("# of Sessions", sessionsc.ToString("#,##0"));

                    //Add message
                    int messagesc = 0;
                    foreach (CopilotStudioBot bot in csbots)
                    {
                        foreach (CopilotStudioSession ses in bot.Sessions)
                        {
                            messagesc = messagesc + ses.Messages.Length;
                        }
                    }
                    t.AddRow("# of Messages, total", messagesc.ToString("#,##0"));

                    //Earliest Session
                    DateTime EarliestStart = DateTime.UtcNow;
                    foreach (CopilotStudioBot bot in csbots)
                    {
                        foreach (CopilotStudioSession session in bot.Sessions)
                        {
                            if (session.ConversationStart < EarliestStart)
                            {
                                EarliestStart = session.ConversationStart;
                            }
                        }
                    }
                    TimeSpan ts = DateTime.UtcNow - EarliestStart;
                    t.AddRow("Oldest session", EarliestStart.ToShortDateString() + " (" + ts.TotalDays.ToString("#,##0") + " days ago)");            

                    //Most recent session
                    DateTime MostRecentSession = EarliestStart.AddYears(-1);
                    foreach (CopilotStudioBot bot in csbots)
                    {
                        foreach (CopilotStudioSession session in bot.Sessions)
                        {
                            if (session.ConversationStart > MostRecentSession)
                            {
                                MostRecentSession = session.ConversationStart;
                            }
                        }
                    }
                    ts = DateTime.UtcNow - MostRecentSession;
                    t.AddRow("Most recent session", MostRecentSession.ToShortDateString() + " (" + ts.TotalDays.ToString("#,##0") + " days ago)");

                    //Messages per day avg
                    TimeSpan ElapsedTimeSinceFirstSession = DateTime.UtcNow - EarliestStart;
                    float msg_per_day = Convert.ToSingle(messagesc) / Convert.ToSingle(ElapsedTimeSinceFirstSession.TotalDays);
                    t.AddRow("Avg. Messages/Day", msg_per_day.ToString("#,##0.0"));

                    AnsiConsole.Write(t);
                }
                else if (DoNextSelection == "See bot ownership breakdown")
                {
                    AnsiConsole.MarkupLine("[bold][underline][navy]Bot Ownership Breakdown, by User[/][/][/]");
                    Console.WriteLine();

                    //Get bot ownership, by owner
                    Dictionary<SystemUser, int> BotOwnership = new Dictionary<SystemUser, int>();
                    foreach (SystemUser user in users)
                    {
                        //Count the # of bots they own
                        int bots_owned = 0;
                        foreach (CopilotStudioBot bot in csbots)
                        {
                            if (bot.Owner == user.SystemUserId)
                            {
                                bots_owned = bots_owned + 1;
                            }
                        }

                        //Log the # that they own if they have at least one
                        if (bots_owned > 0)
                        {
                            BotOwnership.Add(user, bots_owned);
                        }
                    }

                    //Sort those
                    Dictionary<SystemUser, int> BotOwnershipSorted = new Dictionary<SystemUser, int>();
                    while (BotOwnership.Count > 0)
                    {
                        KeyValuePair<SystemUser, int> Winner = BotOwnership.First();
                        foreach (KeyValuePair<SystemUser, int> item in BotOwnership)
                        {
                            if (item.Value > Winner.Value)
                            {
                                Winner = item;
                            }
                        }
                        BotOwnershipSorted.Add(Winner.Key, Winner.Value);
                        BotOwnership.Remove(Winner.Key);
                    }

                    //Write a Spectre.Console breakdown chart
                    BreakdownChart bc = new BreakdownChart();
                    bc.FullSize();
                    foreach (KeyValuePair<SystemUser, int> kvp in BotOwnershipSorted)
                    {
                        bc.AddItem(kvp.Key.FullName, kvp.Value, Color.Blue);
                    }
                    AnsiConsole.Write(bc);
                }
                else if (DoNextSelection == "Examine an individual bot's usage")
                {
                    Tree root = new Tree("Bots in environment '" + auth.Resource + "'");
            
                    //Write a bit of info about each bot in a tree
                    foreach (CopilotStudioBot csbot in csbots)
                    {
                        TreeNode botnode = root.AddNode("[bold][blue]" + csbot.Name + "[/][/] [grey46][italic](" + csbot.SchemaName + ")[/][/]");

                        //Add # of sessions
                        TreeNode sessions = botnode.AddNode("[bold]" + csbot.Sessions.Length.ToString() + "[/] sessions");

                        //Add # of messages
                        TreeNode messages = botnode.AddNode("[bold]" + csbot.MessageCount.ToString("#,##0") + "[/] exchanged messages");

                        //Add # of citations
                        int citations = 0;
                        foreach (CopilotStudioSession ses in csbot.Sessions)
                        {
                            citations = citations + ses.Citations.Length;
                        }
                        TreeNode ncitations = botnode.AddNode("[bold]" + citations.ToString("#,##0") + "[/] citations");
                    }
                    AnsiConsole.Write(root);

                    //Offer a list of bots to pick from
                    SelectionPrompt<string> BotPick = new SelectionPrompt<string>();
                    Console.WriteLine();
                    BotPick.Title("Which bot would you like to learn more about?");
                    foreach (CopilotStudioBot csbot in csbots)
                    {
                        BotPick.AddChoice("[bold][blue]" + csbot.Name + "[/][/] [grey46][italic](" + csbot.SchemaName + ")[/][/]");
                    }
                    BotPick.AddChoice("Go back to main menu");
                    string PickedBot = AnsiConsole.Prompt(BotPick);

                    //If they selected to go back to main menu, go back
                    if (PickedBot == "Go back to main menu")
                    {
                        break; //Will this work?
                    }

                    //Handle bot pick
                    //Find the bot they selected
                    foreach (CopilotStudioBot csbot in csbots)
                    {
                        if (PickedBot.Contains(csbot.SchemaName)) //If it is a match (using SchemaName to check)
                        {
                            //Clear
                            Console.Clear();

                            Tree broot = new Tree("Bot [bold][blue]" + csbot.Name + "[/][/] [grey46][italic](" + csbot.SchemaName + ")[/][/]");

                            //Add # of sessions
                            TreeNode sessions = broot.AddNode("[bold]" + csbot.Sessions.Length.ToString() + "[/] sessions");

                            //Add # of messages
                            TreeNode msgs = broot.AddNode("[bold]" + csbot.MessageCount.ToString("#,##0") + "[/] messages");

                            //Owner
                            foreach (SystemUser user in users)
                            {
                                if (user.SystemUserId == csbot.Owner)
                                {
                                    TreeNode ownernode = broot.AddNode("Owned by [bold]" + user.FullName + "[/] (" + user.Email + ")");
                                }
                            }

                            //Oldest session
                            CopilotStudioSession? OldestSession = csbot.OldestSession;
                            if (OldestSession != null)
                            {
                                broot.AddNode("Oldest Session: " + OldestSession.ConversationStart.ToShortDateString());
                            }

                            //Newest (most recent session)
                            CopilotStudioSession? MostRecentSession = csbot.NewestSession;
                            if (MostRecentSession != null)
                            {
                                broot.AddNode("Newest (most recent) session: " + MostRecentSession.ConversationStart.ToShortDateString());
                            }

                            // Print the tree
                            AnsiConsole.Write(broot);

                            //Ask which transcript they want to analyze
                            SelectionPrompt<string> SessionSelection = new SelectionPrompt<string>();
                            SessionSelection.Title("What session would you like to further analyze?");
                            foreach (CopilotStudioSession ses in csbot.Sessions)
                            {
                                SessionSelection.AddChoice("Session '" + ses.SessionId.ToString() + "' from [bold]" + ses.ConversationStart.ToShortDateString() + "[/] - [bold]" + ses.MessageCount.ToString("#,##0") + "[/] exchanged messages");
                            }
                            Console.WriteLine(); //Buffer room
                            string SessionSelectionChoice = AnsiConsole.Prompt(SessionSelection);

                            //Handle what transcript they selected
                            foreach (CopilotStudioSession ses in csbot.Sessions)
                            {
                                if (SessionSelectionChoice.Contains(ses.SessionId.ToString())) //If there is a match, using the ID showing up in the selection option
                                {
                                    Console.Clear(); //Clear

                                    AnsiConsole.MarkupLine("[bold][underline][blue]Transcript of session '" + ses.SessionId.ToString() + "' with bot '" + csbot.Name + "'[/][/][/]");

                                    //Create a table
                                    Table ChatTable = new Table();
                                    ChatTable.Width(Convert.ToInt32(Convert.ToSingle(Console.WindowWidth) / 1.5f));
                                    ChatTable.Border = TableBorder.Horizontal;

                                    //Add agent
                                    TableColumn tc_agent = new TableColumn("Agent");
                                    tc_agent.Alignment = Justify.Left;
                                    ChatTable.AddColumn(tc_agent);

                                    //Add human
                                    TableColumn tc_human = new TableColumn("Human");
                                    tc_human.Alignment = Justify.Right;
                                    ChatTable.AddColumn(tc_human);
                                    

                                    //Print every message
                                    foreach (CopilotStudioMessage msg in ses.Messages)
                                    {
                                        string txt_to_show = msg.Text.Replace("[", "[[").Replace("]", "]]");
                                        if (msg.Role == "human")
                                        {
                                            ChatTable.AddRow("", txt_to_show);
                                        }
                                        else if (msg.Role == "bot")
                                        {
                                            ChatTable.AddRow(txt_to_show, "");
                                        }

                                        //Add a buffer space
                                        ChatTable.AddRow("", "");
                                    }

                                    //Show the table!
                                    AnsiConsole.Write(ChatTable);
                                }
                            }



                        }
                    }




                }
                else if (DoNextSelection == "Exit")
                {
                    Environment.Exit(0);
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]I'm not sure I understand that! Sorry![/]");
                }

                //Wait for them to press enter before clearing
                Console.WriteLine();
                AnsiConsole.Markup("[gray][italic]Enter to continue...[/][/]");
                Console.ReadLine();
                Console.Clear();
            }

            
            


            # endregion







            
        }
    }
}