# @wip: written, run once, and NOT yet proving what it was written to prove.
#
# The intent. Switching language is what empties LanguageDatabase.activeLanguage, and a dashboard
# snapshot published in that window used to throw out of the runner and end the run with
# exitReason "infrastructure-error", blaming whichever scenario was running. A scenario that goes
# through the switch and is still standing afterwards would be the regression test for it.
#
# What the one run on 2026-09-21 showed, against a build that already carries the fix:
#   - switching to French took about 7 seconds and succeeded;
#   - switching back to English never completed within 35 seconds, so the first scenario fails
#     and the second reads the language as French;
#   - the log holds no "No active language!" line at all, so the run never entered the window the
#     fix is about. It survived because nothing hit it, not because the fix held.
#
# So this cannot yet tell the fixed code from the broken one, and it fails for a reason that is not
# understood: the step, or the game refusing to select English while a session is in French. Both
# need running against the unfixed build before this file claims anything. Until then it stays out
# of default runs, which is what @wip is for.
@wip
Feature: language steps

  Scenario: switching language and coming back leaves the game with a language
    When the language is set to "French"
    Then the language is "French"
    When the language is set to "English"
    Then the language is "English"

  Scenario: the run survived the switch
    Then the language is "English"
    And no errors were logged
