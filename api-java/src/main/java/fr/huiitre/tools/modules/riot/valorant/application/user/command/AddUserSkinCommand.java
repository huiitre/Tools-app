package fr.huiitre.tools.modules.riot.valorant.application.user.command;

public class AddUserSkinCommand {

    private Long skinId;
    private Long accountId;

    public Long getSkinId() {
        return skinId;
    }

    public void setSkinId(Long skinId) {
        this.skinId = skinId;
    }

    public Long getAccountId() {
        return accountId;
    }

    public void setAccountId(Long accountId) {
        this.accountId = accountId;
    }
}
