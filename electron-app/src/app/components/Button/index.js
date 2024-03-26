import React from "react";
import styled from "styled-components";
import loaderGif from "../../assets/loader.gif";

const StyledButton = styled.button`
  border-radius: 0.3125rem;
  background-color: ${({ primary, theme, outline }) =>
    primary ? theme.colors.black : outline ? theme.colors.fullWhite : theme.colors.lightGrey};
  font-style: normal;
  line-height: 1.125rem;
  letter-spacing: -0.0208rem;
  outline: 0;
  border-width: ${(props) => props.outline ? "1px" : "0"};
  cursor: ${(props) => props.disabled ? "not-allowed" : "pointer"};
  color: ${(props) => props.outline ? props.theme.colors.black : props.theme.colors.fullWhite};
  width: 100%;
  text-align: center;
  display: flex;
  flex-direction: row;
  justify-content: flex-start;
  align-items: center;
  padding: 0.69rem;
  min-height: 2.5rem;
  border-color: ${(props) => props.outline ?  props.theme.colors.black : props.theme.colors.transparent};
  border-style: solid;
  opacity: ${(props) => (props.isLoading || props.disabled) ? "0.5" : 1};
  &:hover {
    filter: invert(${(props) => props.hover ? 1 : 0});
  }
`;

const StyledButtonText = styled.p`
  // font-family: ${(props) => props.theme.font};
  font-size: ${(props) => props.theme.fontSizes.tiny};
  font-weight: ${(props) => props.weight};
  line-height: 1.125rem;
  flex: 1;
  margin: 0;
  padding: 0;
`;

const Button = ({
  type,
  primary,
  outline,
  title,
  image,
  imageWidth,
  imageHeight,
  customButtonStyle,
  customButtonTextStyle,
  onPress,
  disabled,
  rightImage,
  rightImageWidth,
  rightImageHeight,
  isLoading,
  hover = false,
  weight = 600,
  ...rest
}) => {
  const handleOnClick = () =>{
    if(isLoading || disabled) return;
    if (onPress) onPress();
  }
  return (
    <StyledButton
      style={customButtonStyle}
      type={type}
      primary={primary}
      outline={outline}
      {...rest}
      onClick={handleOnClick}
      disabled={disabled}
      isLoading={isLoading}
      hover={hover}
    >
      {
        isLoading &&
        <img
          alt="load"
          width={20}
          height={20}
          src={loaderGif}
        />
      }
      {image ? (
        <img
          alt="icon"
          width={imageWidth || 20}
          height={imageHeight || 20}
          src={image}
        />
      ) : null}
      <StyledButtonText weight={weight} style={customButtonTextStyle}>{title}</StyledButtonText>
      {rightImage ? (
        <img
          alt="icon"
          width={rightImageWidth || 20}
          height={rightImageHeight || 20}
          src={rightImage}
        />
      ) : null}
    </StyledButton>
  );
};

export default Button;
