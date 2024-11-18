# Timer

Timer logs and calculates the time spent on a story based on your logging. The summary of your work is displayed for the current and previous weeks. This version is designed for programmers.
<br>
Download the executable: https://drive.google.com/drive/folders/1jRGDC-J8TB_M-BRELUQnkWROjGm3s4oI?usp=sharing
<br><br>
![Screenshot](https://github.com/AndreiVaida/Timer/blob/Programming/Resources/Screenshot%202024-11-18.png?raw=true "Screenshot")
## Instructions
1. Input the activity name in the top-left text-box (it can be the Jira ID), or select an existing activity!
   - Timer loads your latest activities at startup, so you can select an existing one.
   - The selected activity background is animated.
2. Click on the "Crează" button to create the activity or to reload an existing one.
   - A `.csv` file with that name will be created in the "Activities" folder.
3. The state of the window is updated with the created/selected activity (in the state in which you left the activity previously).
4. Click on the 9 buttons to start a "step" of your work:
   - "**👨‍👩‍👧‍👦 Ceremonie**" - Attend a Scrum Ceremony or any other meeting that is not focused on your activity.
   - "**👨‍🎓 Altele**" - Do something unrelated to your activity.
   - "**🤔 Investighez**" - Investigate/troubleshoot the solution/problem (alone or with others).
   - "**✏ Implementez**" - Implement the task/fix (coding).
   - "**⏳ Aștept Review**" - Waiting for your commit to be reviewed. <br>_Note: This is a parallel step, so you can do other "steps" meanwhile, for example reviewing others' commits._
   - "**✏ Rezolv comentarii**" - Resolve the comments received for your commit. <br>_Note: The "⏳ Aștept Review" step is automatically stopped._
   - "**✏ Fac Review**" - You do code review to other teammates.
   - "**⏳ Aștept procesare**" - Wait for a build to execute, a file to download etc.  <br>_Note: This is a parallel step, so you can do other "steps" meanwhile._
   - "**⏸️ Pauză**" - Take a break or finish the working day.
   <br>
   You can click the buttons in any order and as many times you want.
5. Clicking a button represents the start time of the step, respectively the end time of the previous step (unless the active step is parallel). The date-time (seconds precision) is recorded in the csv file. **You can edit the file if you need to correct the logs.**
6. While a step is active, its button will look like it's pressed.
7. The current duration of the active step and the total duration of all steps are calculated and displayed in real time under each button. If there are multiple active steps at the same time, the total time is increased as there was only one active step.
The duration is not saved in the file. Only the Pause step is not considered work.
8. As there are parallel tasks ("⏳ Aștept Review" and "⏳ Aștept procesare") you can have up to 3 active buttons at the same time.
9. Clicking a parallel button stops the sequential active step. Clicking a sequential or a parallel button does not stop a parallel button.
10. Clicking the "⏸️ Pauză" button stops all the other buttons.

## Week summary
The bottom half of the window displays the activities you worked on each day, along with how much you worked on them in that day and how much time you worked in total in that day.
<br>
By default the current week is displayed. You can navigate to previous/next week using the `←` and `→` buttons. Weekends are not displayed.
<br>
💡 Click on an activity to copy its name! Its button will change its name for 2 seconds to confirm the copy action, and a sound will pop you.
<br>
⚠️ The week summary is not yet updated in real-time. Please close and reopen the application to compute the summary of the current day!

## Benefits and an inconvenience:
- You will discover exactly how you spend your time in a working day.
- Makes you more responsible in managing your time.
- Forces you to click the buttons every time you start a new step.