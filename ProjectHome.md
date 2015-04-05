<table cellpadding='0' border='0' cellspacing='0'>
<tr>
<td>
<i>New build 0.3.2 is available! It's a bugfix release: fixed text search and updating of frozen marks (<a href='ReleaseNotes.md'>release notes</a>).</i>
</td>
<td width='50px'> </td>
<td width='100px'>
</td>
</tr>
</table>

---


# WordLight #
## About ##
WordLight is a small add-in for Visual Studio 2008. It searches and highlights substrings that are currently selected in a text editor.

As a bonus, it works for Output, Command and Immediate windows too.

## Usage ##
It is pretty simply: when you selects a text, the add-in highlights all occurrences in a current document.

There is "Freeze search" feature: you can freeze up to three search results in special mark groups, that stay during text editing. Freezing is called by next hotkeys (by default): ``Ctrl+```, `Ctrl+1` and `Ctrl+2` for three groups. For example: you select a text, and all its occurences are marked by pink color automatically. Then you press ``Ctrl+```, and these marks moved to the blue group. To clear a freezing group just press a hotkey when nothing is selected.


## Installation ##
  1. Unpack files to a folder for add-ins of your visual studio.
  1. Restart the studio.
Usually, the folder is placed in "C:\Users\UserName\Documents\Visual Studio 2008" (for Win7) or "C:\Documents and Settings\UserName\My Documents\Visual Studio 2008\Addins" (for WinXP). It can be checked by "Add-in file paths" settings of the studio (menu Tools > Options > Environment > Add-in/Macros Security).

Please note, the add-in requires installed .NET Framework 3.5 to run.

## Settings ##
You can change settings of the add-in by a command "WordLight settings..." under the "Tools" menu.
  * Colors of marks for occurences;
  * Hotkeys to freeze a search;
  * Enable/disable case sensitive searching.
  * Matching only whole words by search.


## Tips and tricks ##
Visual Studio 2008 has various shortcut keys for text search, which look nice with WordLight:
  * `Ctrl+Shift+W` - selects a word at a text cursor. I recommend to remap it to `Ctrl+W`;
  * `Ctrl+F` - opens a Find dialog and puts a word at a text cursor into a search field;
  * `F3` / `Shift+F3` - go to a next/previous occurrence of a search text;
  * `Ctrl+D` - puts a word at a text cursor into a Find combo-box at a toolbar;
  * `Ctrl+F3` / `Ctrl+Shift+F3` - puts a word at a text cursor into the Find combo-box and go to a next/previous its occurrence;
  * `Ctrl+I` - Incremental search: waits for your input and searches as you type a text.

---

