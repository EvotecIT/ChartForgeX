# The shape Get-ADUser -Properties Title, Manager returns.
$users = @'
SamAccountName,Name,Title,Manager,Enabled
avery,Avery Stone,Head of IT,,True
morgan,Morgan Lee,Infrastructure lead,avery,True
sam,Sam Park,Service desk lead,avery,True
alex,Alex Kim,Network engineer,morgan,True
jordan,Jordan Bell,Cloud engineer,morgan,False
casey,Casey Ward,Analyst,sam,True
robin,Robin Shaw,Analyst,sam,True
'@ | ConvertFrom-Csv

$members = {
    foreach ($user in $users) {
        $member = @{
            Id     = $user.SamAccountName
            Name   = $user.Name
            Role   = $user.Title
            Status = if ($user.Enabled -eq 'True') { 'Healthy' }
                     else { 'Warning' }
        }
        if ($user.Manager) { $member.ParentId = $user.Manager }
        New-ImageOrganizationMember @member
    }
}
New-ImageOrganizationChart $members -TeamLabel 'IT department' `
    -Subtitle 'Disabled accounts flagged' -NoTeamNode `
    -FilePath org-chart.png -Width 960 -Height 420

$users | Format-Table Name, Title, Manager, Enabled -AutoSize
