import { colors } from "../../themes/constant";
import Button from "../../components/Button";

export default function UpgradePlan(closeApplication, onSubmitGoToPayment) {
  return (
    <>
      <p>
        You’re currently on Free plan, please upgrade to continue using
        Snaptrude
      </p>
      <footer>
        <div className="button-wrapper">
          <Button
            customButtonStyle={{
              backgroundColor: colors.fullWhite,
              color: colors.secondaryGrey,
            }}
            title={"Cancel"}
            onPress={closeApplication}
          />
        </div>
        <div className="button-wrapper">
          <Button
            primary={true}
            title={"Upgrade Plan"}
            onPress={onSubmitGoToPayment}
          />
        </div>
      </footer>
    </>
  );
}
